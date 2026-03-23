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
    private readonly TimeSpan _allowedLateness = TimeSpan.FromSeconds(Math.Max(0, batchingOptions.Value.AllowedLatenessSeconds));

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var windows = new SortedDictionary<DateTimeOffset, List<TradeRow>>();
        DateTimeOffset? nextWindowStartToFlush = null;
        DateTimeOffset? lastFlushedWindowStart = null;
        DateTimeOffset? latestObservedWindowStart = null;

        await using var enumerator = streamClient.ReadMessagesAsync(cancellationToken).GetAsyncEnumerator(cancellationToken);
        Task<bool>? nextMessageTask = null;

        while (!cancellationToken.IsCancellationRequested)
        {
            nextMessageTask ??= enumerator.MoveNextAsync().AsTask();

            if (nextWindowStartToFlush is null)
            {
                if (!await nextMessageTask)
                {
                    break;
                }

                ProcessMessage(enumerator.Current);
                nextMessageTask = null;
                await FlushExpiredWindowsAsync(clock.UtcNow, cancellationToken);
                continue;
            }

            var nextFlushAt = nextWindowStartToFlush.Value.Add(_window).Add(_allowedLateness);
            var remaining = nextFlushAt - clock.UtcNow;

            if (remaining <= TimeSpan.Zero)
            {
                await FlushExpiredWindowsAsync(clock.UtcNow, cancellationToken);
                continue;
            }

            var delayTask = Task.Delay(remaining, cancellationToken);
            var completedTask = await Task.WhenAny(nextMessageTask, delayTask);

            if (completedTask == delayTask)
            {
                await FlushExpiredWindowsAsync(clock.UtcNow, cancellationToken);
                continue;
            }

            if (!await nextMessageTask)
            {
                break;
            }

            ProcessMessage(enumerator.Current);
            nextMessageTask = null;
            await FlushExpiredWindowsAsync(clock.UtcNow, cancellationToken);
        }

        await FlushRemainingWindowsAsync(cancellationToken);
        return;

        void ProcessMessage(string message)
        {
            if (!parser.TryParse(message, out var trade) || trade is null)
            {
                logger.LogWarning("Se descarto un mensaje invalido del stream.");
                return;
            }

            var tradeWindowStart = AlignToWindowStart(trade.TradeTimeUtc);

            if (lastFlushedWindowStart.HasValue && tradeWindowStart <= lastFlushedWindowStart.Value)
            {
                logger.LogWarning(
                    "Se descarto un trade demasiado tardio para una ventana ya cerrada. TradeTimeUtc={TradeTimeUtc:O}, WindowStart={WindowStart:O}.",
                    trade.TradeTimeUtc,
                    tradeWindowStart);
                return;
            }

            if (latestObservedWindowStart.HasValue && tradeWindowStart < latestObservedWindowStart.Value)
            {
                logger.LogInformation(
                    "Se acepto un trade tardio/fuera de orden para la ventana {WindowStart:O}.",
                    tradeWindowStart);
            }

            latestObservedWindowStart = latestObservedWindowStart.HasValue && latestObservedWindowStart.Value > tradeWindowStart
                ? latestObservedWindowStart
                : tradeWindowStart;

            if (!windows.TryGetValue(tradeWindowStart, out var trades))
            {
                trades = [];
                windows[tradeWindowStart] = trades;
            }

            if (!nextWindowStartToFlush.HasValue || tradeWindowStart < nextWindowStartToFlush.Value)
            {
                nextWindowStartToFlush = tradeWindowStart;
            }

            trades.Add(trade);
        }

        async Task FlushExpiredWindowsAsync(DateTimeOffset now, CancellationToken flushCancellationToken)
        {
            while (nextWindowStartToFlush.HasValue &&
                   nextWindowStartToFlush.Value.Add(_window).Add(_allowedLateness) <= now)
            {
                var windowStart = nextWindowStartToFlush.Value;
                await FlushWindowAsync(windowStart, flushCancellationToken);
                lastFlushedWindowStart = windowStart;
                nextWindowStartToFlush = windowStart.Add(_window);

                if (windows.Count == 0 && latestObservedWindowStart.HasValue && nextWindowStartToFlush > latestObservedWindowStart.Value)
                {
                    nextWindowStartToFlush = null;
                }
            }
        }

        async Task FlushRemainingWindowsAsync(CancellationToken flushCancellationToken)
        {
            if (!nextWindowStartToFlush.HasValue)
            {
                return;
            }

            var finalWindowStart = windows.Count > 0
                ? windows.Keys.Max()
                : nextWindowStartToFlush.Value;

            while (nextWindowStartToFlush.HasValue && nextWindowStartToFlush.Value <= finalWindowStart)
            {
                var windowStart = nextWindowStartToFlush.Value;
                await FlushWindowAsync(windowStart, flushCancellationToken);
                lastFlushedWindowStart = windowStart;
                nextWindowStartToFlush = windowStart.Add(_window);
            }

            nextWindowStartToFlush = null;
        }

        async Task FlushWindowAsync(DateTimeOffset windowStart, CancellationToken flushCancellationToken)
        {
            var windowEnd = windowStart.Add(_window);

            if (!windows.Remove(windowStart, out var trades) || trades.Count == 0)
            {
                logger.LogWarning(
                    "No hubo trades para la ventana {WindowStart:O} - {WindowEnd:O}.",
                    windowStart,
                    windowEnd);
                return;
            }

            logger.LogInformation(
                "Se cerro la ventana {WindowStart:O} - {WindowEnd:O} con {Count} trades.",
                windowStart,
                windowEnd,
                trades.Count);

            await batchProcessor.ProcessBatchAsync(trades, windowStart, windowEnd, flushCancellationToken);
        }
    }

    private DateTimeOffset AlignToWindowStart(DateTimeOffset timestamp)
    {
        var ticks = _window.Ticks;
        var alignedTicks = timestamp.UtcTicks - (timestamp.UtcTicks % ticks);
        return new DateTimeOffset(alignedTicks, TimeSpan.Zero);
    }
}
