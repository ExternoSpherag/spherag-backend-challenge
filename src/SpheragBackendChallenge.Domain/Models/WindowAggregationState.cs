namespace SpheragBackendChallenge.Domain.Models;

using SpheragBackendChallenge.Domain.Entities;

public sealed class WindowAggregationState
{
    private readonly object _syncRoot = new();
    private readonly HashSet<long> _processedTradeIds = [];
    private decimal _sumOfPrices;
    private long _tradeCount;

    public WindowAggregationState(WindowKey key, DateTime windowEndUtc, DateTime closeAfterUtc)
    {
        Key = key;
        WindowEndUtc = windowEndUtc;
        CloseAfterUtc = closeAfterUtc;
    }

    public WindowKey Key { get; }

    public DateTime WindowEndUtc { get; }

    public DateTime CloseAfterUtc { get; }

    public long TradeCount
    {
        get
        {
            lock (_syncRoot)
            {
                return _tradeCount;
            }
        }
    }

    public bool TryAddTrade(decimal price, long? tradeId)
    {
        lock (_syncRoot)
        {
            if (tradeId.HasValue && !_processedTradeIds.Add(tradeId.Value))
            {
                return false;
            }

            _sumOfPrices += price;
            _tradeCount++;
            return true;
        }
    }

    public AggregatedPrice? ToAggregatedPrice(DateTime createdAtUtc)
    {
        lock (_syncRoot)
        {
            if (_tradeCount == 0)
            {
                return null;
            }

            return new AggregatedPrice
            {
                Symbol = Key.Symbol,
                WindowStartUtc = Key.WindowStartUtc,
                WindowEndUtc = WindowEndUtc,
                AveragePrice = _sumOfPrices / _tradeCount,
                TradeCount = _tradeCount,
                CreatedAtUtc = createdAtUtc
            };
        }
    }
}
