using RealtimeMarketData.Domain.Primitives;

namespace RealtimeMarketData.Domain.Aggregates.PriceWindow;

public sealed class PriceWindow : AuditableEntity<Guid>
{
    private PriceWindow() { } // EF Core

    private PriceWindow(
        Guid id,
        string symbol,
        DateTimeOffset windowStart,
        DateTimeOffset windowEnd,
        decimal averagePrice,
        int tradeCount)
        : base(id)
    {
        Symbol = symbol;
        WindowStart = windowStart;
        WindowEnd = windowEnd;
        AveragePrice = averagePrice;
        TradeCount = tradeCount;
    }

    public string Symbol { get; private set; } = null!;
    public DateTimeOffset WindowStart { get; private set; }
    public DateTimeOffset WindowEnd { get; private set; }
    public decimal AveragePrice { get; private set; }
    public int TradeCount { get; private set; }

    public static PriceWindow Create(
        Guid id,
        string symbol,
        DateTimeOffset windowStart,
        DateTimeOffset windowEnd,
        decimal averagePrice,
        int tradeCount)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol, nameof(symbol));
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(averagePrice, 0m, nameof(averagePrice));
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(tradeCount, 0, nameof(tradeCount));

        if (windowEnd <= windowStart)
            throw new ArgumentException("WindowEnd must be after WindowStart.", nameof(windowEnd));

        return new PriceWindow(id, symbol, windowStart, windowEnd, averagePrice, tradeCount);
    }

    /// <summary>
    /// Updates the persisted window with the latest aggregation snapshot values.
    /// Called when a new trade arrives within an already-persisted window.
    /// </summary>
    public void ApplySnapshot(decimal averagePrice, int tradeCount)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(averagePrice, 0m, nameof(averagePrice));
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(tradeCount, 0, nameof(tradeCount));

        AveragePrice = averagePrice;
        TradeCount = tradeCount;
    }
}