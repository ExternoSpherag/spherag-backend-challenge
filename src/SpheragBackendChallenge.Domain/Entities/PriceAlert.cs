namespace SpheragBackendChallenge.Domain.Entities;

public sealed class PriceAlert
{
    public long Id { get; set; }

    public string Symbol { get; set; } = string.Empty;

    public decimal PreviousAveragePrice { get; set; }

    public decimal CurrentAveragePrice { get; set; }

    public decimal PercentageChange { get; set; }

    public DateTime WindowStartUtc { get; set; }

    public DateTime WindowEndUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public static PriceAlert? CreateIfThresholdExceeded(
        AggregatedPrice previous,
        AggregatedPrice current,
        decimal thresholdPercentage,
        DateTime createdAtUtc)
    {
        ArgumentNullException.ThrowIfNull(previous);
        ArgumentNullException.ThrowIfNull(current);

        var percentageChange = current.CalculatePercentageChangeFrom(previous);

        if (decimal.Abs(percentageChange) <= thresholdPercentage)
        {
            return null;
        }

        return new PriceAlert
        {
            Symbol = current.Symbol,
            PreviousAveragePrice = previous.AveragePrice,
            CurrentAveragePrice = current.AveragePrice,
            PercentageChange = percentageChange,
            WindowStartUtc = current.WindowStartUtc,
            WindowEndUtc = current.WindowEndUtc,
            CreatedAtUtc = createdAtUtc
        };
    }
}
