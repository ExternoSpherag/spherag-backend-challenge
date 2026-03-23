namespace SpheragBackendChallenge.Domain.Entities;

public sealed class AggregatedPrice
{
    public long Id { get; set; }

    public string Symbol { get; set; } = string.Empty;

    public DateTime WindowStartUtc { get; set; }

    public DateTime WindowEndUtc { get; set; }

    public decimal AveragePrice { get; set; }

    public long TradeCount { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public decimal CalculatePercentageChangeFrom(AggregatedPrice previous)
    {
        ArgumentNullException.ThrowIfNull(previous);

        if (previous.AveragePrice == 0m)
        {
            return 0m;
        }

        return ((AveragePrice - previous.AveragePrice) / previous.AveragePrice) * 100m;
    }
}
