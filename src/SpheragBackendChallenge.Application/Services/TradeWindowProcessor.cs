using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SpheragBackendChallenge.Application.Configuration;
using SpheragBackendChallenge.Application.Interfaces;
using SpheragBackendChallenge.Domain.Entities;
using SpheragBackendChallenge.Domain.Models;
using System.Collections.Concurrent;

namespace SpheragBackendChallenge.Application.Services;

public sealed class TradeWindowProcessor(
    IServiceScopeFactory serviceScopeFactory,
    IOptions<TradeProcessingOptions> options,
    ILogger<TradeWindowProcessor> logger) : ITradeWindowProcessor
{
    private readonly ConcurrentDictionary<WindowKey, WindowAggregationState> _windows = new();
    private readonly TradeProcessingOptions _options = options.Value;
    private readonly TimeSpan _windowSize = TimeSpan.FromSeconds(options.Value.WindowSizeSeconds);
    private readonly TimeSpan _allowedLateness = TimeSpan.FromSeconds(options.Value.AllowedLatenessSeconds);

    public bool TryProcessTrade(TradeEvent tradeEvent, DateTime nowUtc)
    {
        var tradeWindow = TradeWindow.FromTradeTimestamp(tradeEvent.TradeTimestampUtc, _windowSize);
        var closeAfterUtc = tradeWindow.EndUtc.Add(_allowedLateness);

        if (nowUtc > closeAfterUtc)
        {
            logger.LogWarning(
                "Discarding late trade for {Symbol}. TradeTimestampUtc={TradeTimestampUtc:o}, WindowEndUtc={WindowEndUtc:o}, CloseAfterUtc={CloseAfterUtc:o}",
                tradeEvent.Symbol,
                tradeEvent.TradeTimestampUtc,
                tradeWindow.EndUtc,
                closeAfterUtc);

            return false;
        }

        var key = new WindowKey(tradeEvent.Symbol, tradeWindow.StartUtc);
        var state = _windows.GetOrAdd(key, _ => new WindowAggregationState(key, tradeWindow.EndUtc, closeAfterUtc));
        var added = state.TryAddTrade(tradeEvent.Price, tradeEvent.TradeId);

        if (!added)
        {
            logger.LogDebug(
                "Duplicate trade ignored within active window. Symbol={Symbol}, TradeId={TradeId}",
                tradeEvent.Symbol,
                tradeEvent.TradeId);

            return false;
        }

        return true;
    }

    public async Task<int> FlushExpiredWindowsAsync(DateTime nowUtc, CancellationToken cancellationToken)
    {
        var flushedCount = 0;
        using var scope = serviceScopeFactory.CreateScope();
        var tradeAggregationRepository = scope.ServiceProvider.GetRequiredService<ITradeAggregationRepository>();
        var priceAlertRepository = scope.ServiceProvider.GetRequiredService<IPriceAlertRepository>();

        foreach (var pair in _windows)
        {
            if (pair.Value.CloseAfterUtc >= nowUtc)
            {
                continue;
            }

            if (!_windows.TryRemove(pair.Key, out var state))
            {
                continue;
            }

            var aggregatedPrice = state.ToAggregatedPrice(nowUtc);

            if (aggregatedPrice is null)
            {
                continue;
            }

            var previousWindow = await tradeAggregationRepository.GetLatestBeforeWindowAsync(
                aggregatedPrice.Symbol,
                aggregatedPrice.WindowStartUtc,
                cancellationToken);

            await tradeAggregationRepository.AddAsync(aggregatedPrice, cancellationToken);

            if (previousWindow is not null)
            {
                var alert = PriceAlert.CreateIfThresholdExceeded(previousWindow, aggregatedPrice, _options.AlertThresholdPercentage, nowUtc);
                if (alert is not null)
                {
                    await priceAlertRepository.AddAsync(alert, cancellationToken);

                    logger.LogWarning(
                        "Price alert for {Symbol}. PreviousAverage={PreviousAverage}, CurrentAverage={CurrentAverage}, PercentageChange={PercentageChange}",
                        alert.Symbol,
                        alert.PreviousAveragePrice,
                        alert.CurrentAveragePrice,
                        alert.PercentageChange);
                }
            }

            flushedCount++;
        }

        return flushedCount;
    }
}
