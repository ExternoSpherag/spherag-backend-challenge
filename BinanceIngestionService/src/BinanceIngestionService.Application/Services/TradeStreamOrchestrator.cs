using BinanceIngestionService.Application.Configuration;
using BinanceIngestionService.Domain.Abstractions;
using BinanceIngestionService.Domain.Entities;
using BinanceIngestionService.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BinanceIngestionService.Application.Services;

public class TradeStreamOrchestrator(
    ITradeStreamClient streamClient,
    ITradeMessageParser parser,
    TradeBatchProcessor batchProcessor,
    IClock clock,
    IOptions<BatchingOptions> batchingOptions,
    ILogger<TradeStreamOrchestrator> logger)
{
    private readonly TimeSpan _window = TimeSpan.FromSeconds(Math.Max(1, batchingOptions.Value.WindowSeconds));

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var trades = new List<TradeRow>();
        DateTimeOffset? currentWindowStart = null;

        await using var enumerator = streamClient.ReadMessagesAsync(cancellationToken).GetAsyncEnumerator(cancellationToken);
        Task<bool>? nextMessageTask = null;

        while (!cancellationToken.IsCancellationRequested)
        {
            nextMessageTask ??= enumerator.MoveNextAsync().AsTask();

            if (currentWindowStart is null || trades.Count == 0)
            {
                if (!await nextMessageTask)
                {
                    break;
                }

                await ProcessMessageAsync(enumerator.Current, trades, cancellationToken);
                nextMessageTask = null;
                continue;
            }

            var windowEnd = currentWindowStart.Value.Add(_window);
            var remaining = windowEnd - clock.UtcNow;

            if (remaining <= TimeSpan.Zero)
            {
                await FlushAsync(trades, currentWindowStart.Value, cancellationToken);
                currentWindowStart = null;
                continue;
            }

            var delayTask = Task.Delay(remaining, cancellationToken);
            var completedTask = await Task.WhenAny(nextMessageTask, delayTask);

            if (completedTask == delayTask)
            {
                await FlushAsync(trades, currentWindowStart.Value, cancellationToken);
                currentWindowStart = null;
                continue;
            }

            if (!await nextMessageTask)
            {
                break;
            }

            await ProcessMessageAsync(enumerator.Current, trades, cancellationToken);
            nextMessageTask = null;
        }

        if (currentWindowStart is not null)
        {
            await FlushAsync(trades, currentWindowStart.Value, cancellationToken);
        }

        async Task ProcessMessageAsync(string message, List<TradeRow> currentTrades, CancellationToken processCancellationToken)
        {
            if (!parser.TryParse(message, out var trade) || trade is null)
            {
                logger.LogWarning("Se descarto un mensaje invalido del stream.");
                return;
            }

            var tradeWindowStart = AlignToWindowStart(trade.TradeTimeUtc);

            if (currentWindowStart is null)
            {
                currentWindowStart = tradeWindowStart;
            }

            if (tradeWindowStart > currentWindowStart.Value)
            {
                await FlushAsync(currentTrades, currentWindowStart.Value, processCancellationToken);
                currentWindowStart = tradeWindowStart;
            }
            else if (tradeWindowStart < currentWindowStart.Value)
            {
                logger.LogWarning(
                    "Se descarto un trade fuera de orden. TradeTimeUtc={TradeTimeUtc:O}, WindowStart={WindowStart:O}.",
                    trade.TradeTimeUtc,
                    currentWindowStart.Value);
                return;
            }

            currentTrades.Add(trade);
        }
    }

    private async Task FlushAsync(
        IReadOnlyCollection<TradeRow> trades,
        DateTimeOffset windowStart,
        CancellationToken cancellationToken)
    {
        var windowEnd = windowStart.Add(_window);

        logger.LogInformation(
            "Se cerro la ventana {WindowStart:O} - {WindowEnd:O} con {Count} trades.",
            windowStart,
            windowEnd,
            trades.Count);

        await batchProcessor.ProcessBatchAsync(trades, windowStart, windowEnd, cancellationToken);

        if (trades is List<TradeRow> tradeList)
        {
            tradeList.Clear();
        }
    }

    private DateTimeOffset AlignToWindowStart(DateTimeOffset timestamp)
    {
        var ticks = _window.Ticks;
        var alignedTicks = timestamp.UtcTicks - (timestamp.UtcTicks % ticks);
        return new DateTimeOffset(alignedTicks, TimeSpan.Zero);
    }
}
