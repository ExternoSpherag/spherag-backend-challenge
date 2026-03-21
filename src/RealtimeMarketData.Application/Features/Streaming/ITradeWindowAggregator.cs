using RealtimeMarketData.Domain.ValueObjects;

namespace RealtimeMarketData.Application.Features.Streaming;

public interface ITradeWindowAggregator
{
    TradeWindowAggregationSnapshot AddTrade(
        Symbol symbol,
        long tradeId,
        decimal tradePrice,
        DateTimeOffset tradeTimestamp);
}

public sealed record TradeWindowAggregationSnapshot(
    Guid AggregationId,
    string Symbol,
    DateTimeOffset WindowStart,
    DateTimeOffset WindowEnd,
    decimal AveragePrice,
    int TradeCount,
    bool IsDuplicate);