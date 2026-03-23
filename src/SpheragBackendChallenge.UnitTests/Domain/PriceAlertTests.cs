using SpheragBackendChallenge.Domain.Entities;

namespace SpheragBackendChallenge.UnitTests.Domain;

public sealed class PriceAlertTests
{
    [Fact]
    public void PriceAlert_IsCreatedWhenChangeExceedsThreshold()
    {
        var previous = new AggregatedPrice
        {
            Symbol = "BTCUSDT",
            AveragePrice = 100m,
            WindowStartUtc = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc),
            WindowEndUtc = new DateTime(2026, 1, 1, 12, 0, 5, DateTimeKind.Utc)
        };

        var current = new AggregatedPrice
        {
            Symbol = "BTCUSDT",
            AveragePrice = 106m,
            WindowStartUtc = new DateTime(2026, 1, 1, 12, 0, 5, DateTimeKind.Utc),
            WindowEndUtc = new DateTime(2026, 1, 1, 12, 0, 10, DateTimeKind.Utc)
        };

        var alert = PriceAlert.CreateIfThresholdExceeded(previous, current, 5m, DateTime.UtcNow);

        Assert.NotNull(alert);
        Assert.Equal(6m, alert!.PercentageChange);
    }

    [Fact]
    public void PriceAlert_IsNotCreatedWhenChangeIsFivePercentOrLess()
    {
        var previous = new AggregatedPrice
        {
            Symbol = "BTCUSDT",
            AveragePrice = 100m,
            WindowStartUtc = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc),
            WindowEndUtc = new DateTime(2026, 1, 1, 12, 0, 5, DateTimeKind.Utc)
        };

        var current = new AggregatedPrice
        {
            Symbol = "BTCUSDT",
            AveragePrice = 105m,
            WindowStartUtc = new DateTime(2026, 1, 1, 12, 0, 5, DateTimeKind.Utc),
            WindowEndUtc = new DateTime(2026, 1, 1, 12, 0, 10, DateTimeKind.Utc)
        };

        var alert = PriceAlert.CreateIfThresholdExceeded(previous, current, 5m, DateTime.UtcNow);

        Assert.Null(alert);
    }
}
