using RealtimeMarketData.Domain.Primitives;
using RealtimeMarketData.Domain.ValueObjects;

namespace RealtimeMarketData.Domain.Aggregates.PriceWindow;

public sealed class WindowPriceAggregation : AggregateRoot<Guid>
{
    private readonly HashSet<long> _processedTradeIds = [];

    private WindowPriceAggregation() { } 

    private WindowPriceAggregation(
        Guid id,
        Symbol symbol,
        FiveSecondWindow window,
        decimal priceSum,
        int tradeCount) : base(id)
    {
        Symbol = symbol;
        Window = window;
        PriceSum = priceSum;
        TradeCount = tradeCount;
    }

    public Symbol Symbol { get; private set; } = null!;
    public FiveSecondWindow Window { get; private set; } = null!;
    public decimal PriceSum { get; private set; }
    public int TradeCount { get; private set; }
    public decimal AveragePrice => TradeCount == 0 ? 0 : PriceSum / TradeCount;

    public static WindowPriceAggregation Create(
        Symbol symbol,
        DateTimeOffset tradeTimestamp,
        long tradeId,
        decimal tradePrice)
    {
        ValidateInput(tradeId, tradePrice);

        var window = FiveSecondWindow.FromTradeTimestamp(tradeTimestamp);
        var aggregate = new WindowPriceAggregation(
            Guid.NewGuid(),
            symbol,
            window,
            tradePrice,
            1);

        aggregate._processedTradeIds.Add(tradeId);
        return aggregate;
    }

    public bool TryAddTrade(
        long tradeId,
        DateTimeOffset tradeTimestamp,
        decimal tradePrice)
    {
        ValidateInput(tradeId, tradePrice);

        var tradeWindow = FiveSecondWindow.FromTradeTimestamp(tradeTimestamp);
        if (tradeWindow != Window)
            throw new InvalidOperationException("Trade does not belong to the current 5-second window.");

        if (!_processedTradeIds.Add(tradeId))
            return false; 

        PriceSum += tradePrice;
        TradeCount += 1;
        return true;
    }

    private static void ValidateInput(long tradeId, decimal tradePrice)
    {
        if (tradeId <= 0)
            throw new ArgumentOutOfRangeException(nameof(tradeId), "Trade id must be greater than zero.");

        if (tradePrice <= 0)
            throw new ArgumentOutOfRangeException(nameof(tradePrice), "Trade price must be greater than zero.");
    }
}