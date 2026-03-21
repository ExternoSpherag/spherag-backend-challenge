namespace RealtimeMarketData.Domain.Aggregates.PriceWindow;

public static class PriceChangeAlertRule
{
    public static bool ShouldTrigger(
        decimal previousAveragePrice,
        decimal currentAveragePrice,
        decimal thresholdPercent)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(thresholdPercent, 0m, nameof(thresholdPercent));

        return CalculateAbsoluteChangePercent(previousAveragePrice, currentAveragePrice) > thresholdPercent;
    }

    public static decimal CalculateAbsoluteChangePercent(decimal previousAveragePrice, decimal currentAveragePrice)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(previousAveragePrice, 0m, nameof(previousAveragePrice));
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(currentAveragePrice, 0m, nameof(currentAveragePrice));

        return Math.Abs((currentAveragePrice - previousAveragePrice) / previousAveragePrice) * 100m;
    }
}