using System.Collections.Concurrent;
using RealtimeMarketData.Application.Features.Streaming;
using RealtimeMarketData.Domain.Aggregates.PriceWindow;
using RealtimeMarketData.Domain.ValueObjects;

namespace RealtimeMarketData.Infrastructure.Aggregation;

internal sealed class InMemoryTradeWindowAggregator : ITradeWindowAggregator
{
    private readonly ConcurrentDictionary<AggregationKey, WindowPriceAggregation> _state = new();

    public TradeWindowAggregationSnapshot AddTrade(
        Symbol symbol,
        long tradeId,
        decimal tradePrice,
        DateTimeOffset tradeTimestamp)
    {
        var window = FiveSecondWindow.FromTradeTimestamp(tradeTimestamp);
        var key = new AggregationKey(symbol.Value, window.Start);

        if (_state.TryGetValue(key, out var existing))
        {
            lock (existing)
            {
                var added = existing.TryAddTrade(tradeId, tradeTimestamp, tradePrice);
                return ToSnapshot(existing, isDuplicate: !added);
            }
        }

        var created = WindowPriceAggregation.Create(symbol, tradeTimestamp, tradeId, tradePrice);

        if (_state.TryAdd(key, created))
            return ToSnapshot(created, isDuplicate: false);

        var winner = _state[key];
        lock (winner)
        {
            var added = winner.TryAddTrade(tradeId, tradeTimestamp, tradePrice);
            return ToSnapshot(winner, isDuplicate: !added);
        }
    }

    private static TradeWindowAggregationSnapshot ToSnapshot(WindowPriceAggregation aggregation, bool isDuplicate) =>
        new(
            aggregation.Id,
            aggregation.Symbol.Value,
            aggregation.Window.Start,
            aggregation.Window.End,
            aggregation.AveragePrice,
            aggregation.TradeCount,
            isDuplicate);

    private readonly record struct AggregationKey(string Symbol, DateTimeOffset WindowStart);
}