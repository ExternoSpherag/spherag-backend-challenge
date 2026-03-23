using SpheragBackendChallenge.Domain.Models;

namespace SpheragBackendChallenge.UnitTests.Domain;

public sealed class TradeWindowTests
{
    [Fact]
    public void TradeWindow_AlignsToWallClockBoundaries()
    {
        var timestampUtc = new DateTime(2026, 1, 1, 12, 0, 9, 750, DateTimeKind.Utc);

        var window = TradeWindow.FromTradeTimestamp(timestampUtc, TimeSpan.FromSeconds(5));

        Assert.Equal(new DateTime(2026, 1, 1, 12, 0, 5, DateTimeKind.Utc), window.StartUtc);
        Assert.Equal(new DateTime(2026, 1, 1, 12, 0, 10, DateTimeKind.Utc), window.EndUtc);
    }
}
