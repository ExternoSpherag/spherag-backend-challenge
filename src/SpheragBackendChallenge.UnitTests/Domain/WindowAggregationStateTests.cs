using SpheragBackendChallenge.Domain.Models;

namespace SpheragBackendChallenge.UnitTests.Domain;

public sealed class WindowAggregationStateTests
{
    [Fact]
    public void WindowAggregationState_ComputesMean()
    {
        var state = new WindowAggregationState(
            new WindowKey("BTCUSDT", new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc)),
            new DateTime(2026, 1, 1, 12, 0, 5, DateTimeKind.Utc),
            new DateTime(2026, 1, 1, 12, 0, 10, DateTimeKind.Utc));

        state.TryAddTrade(100m, 1);
        state.TryAddTrade(200m, 2);
        state.TryAddTrade(300m, 3);

        var aggregatedPrice = state.ToAggregatedPrice(DateTime.UtcNow);

        Assert.NotNull(aggregatedPrice);
        Assert.Equal(3, aggregatedPrice!.TradeCount);
        Assert.Equal(200m, aggregatedPrice.AveragePrice);
    }

    [Fact]
    public void WindowAggregationState_IgnoresDuplicateTradeIdsWithinTheSameWindow()
    {
        var state = new WindowAggregationState(
            new WindowKey("BTCUSDT", new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc)),
            new DateTime(2026, 1, 1, 12, 0, 5, DateTimeKind.Utc),
            new DateTime(2026, 1, 1, 12, 0, 10, DateTimeKind.Utc));

        var firstAdd = state.TryAddTrade(100m, 1);
        var secondAdd = state.TryAddTrade(100m, 1);

        var aggregatedPrice = state.ToAggregatedPrice(DateTime.UtcNow);

        Assert.True(firstAdd);
        Assert.False(secondAdd);
        Assert.NotNull(aggregatedPrice);
        Assert.Equal(1, aggregatedPrice!.TradeCount);
        Assert.Equal(100m, aggregatedPrice.AveragePrice);
    }

    [Fact]
    public void WindowAggregationState_DoesNotDeduplicateTradesWithoutTradeId()
    {
        var state = new WindowAggregationState(
            new WindowKey("BTCUSDT", new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc)),
            new DateTime(2026, 1, 1, 12, 0, 5, DateTimeKind.Utc),
            new DateTime(2026, 1, 1, 12, 0, 10, DateTimeKind.Utc));

        var firstAdd = state.TryAddTrade(100m, null);
        var secondAdd = state.TryAddTrade(200m, null);

        var aggregatedPrice = state.ToAggregatedPrice(DateTime.UtcNow);

        Assert.True(firstAdd);
        Assert.True(secondAdd);
        Assert.NotNull(aggregatedPrice);
        Assert.Equal(2, aggregatedPrice!.TradeCount);
        Assert.Equal(150m, aggregatedPrice.AveragePrice);
    }
}
