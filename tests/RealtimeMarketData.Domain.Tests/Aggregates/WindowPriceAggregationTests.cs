using FluentAssertions;
using RealtimeMarketData.Domain.Aggregates.PriceWindow;
using RealtimeMarketData.Domain.ValueObjects;
using Xunit;

namespace RealtimeMarketData.Domain.Tests.Aggregates;

public sealed class WindowPriceAggregationTests
{
    [Fact]
    public void Create_AndTryAddTrade_InSameWindow_ShouldRecalculateAverage()
    {
        var symbol = Symbol.Create("BTCUSDT");
        var firstTimestamp = DateTimeOffset.Parse("2026-01-01T12:00:10.100Z");
        var secondTimestamp = DateTimeOffset.Parse("2026-01-01T12:00:14.900Z");

        var aggregation = WindowPriceAggregation.Create(
            symbol,
            firstTimestamp,
            tradeId: 1001,
            tradePrice: 100m);

        var added = aggregation.TryAddTrade(
            tradeId: 1002,
            tradeTimestamp: secondTimestamp,
            tradePrice: 200m);

        added.Should().BeTrue();
        aggregation.TradeCount.Should().Be(2);
        aggregation.AveragePrice.Should().Be(150m);
        aggregation.Window.Start.Should().Be(DateTimeOffset.Parse("2026-01-01T12:00:10Z"));
        aggregation.Window.End.Should().Be(DateTimeOffset.Parse("2026-01-01T12:00:15Z"));
    }

    [Fact]
    public void TryAddTrade_WithDuplicateTradeId_ShouldBeIdempotent()
    {
        var symbol = Symbol.Create("BTCUSDT");
        var timestamp = DateTimeOffset.Parse("2026-01-01T12:00:10.100Z");

        var aggregation = WindowPriceAggregation.Create(
            symbol,
            timestamp,
            tradeId: 777,
            tradePrice: 100m);

        var added = aggregation.TryAddTrade(
            tradeId: 777,
            tradeTimestamp: DateTimeOffset.Parse("2026-01-01T12:00:11.000Z"),
            tradePrice: 999m);

        added.Should().BeFalse();
        aggregation.TradeCount.Should().Be(1);
        aggregation.AveragePrice.Should().Be(100m);
    }

    [Fact]
    public void TryAddTrade_WithTradeInDifferentWindow_ShouldThrow()
    {
        var symbol = Symbol.Create("BTCUSDT");

        var aggregation = WindowPriceAggregation.Create(
            symbol,
            DateTimeOffset.Parse("2026-01-01T12:00:10.100Z"),
            tradeId: 1,
            tradePrice: 100m);

        var act = () => aggregation.TryAddTrade(
            tradeId: 2,
            tradeTimestamp: DateTimeOffset.Parse("2026-01-01T12:00:15.000Z"),
            tradePrice: 200m);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Trade does not belong to the current 5-second window.");
    }

    [Fact]
    public void Create_ShouldAssignIdentity()
    {
        var symbol = Symbol.Create("BTCUSDT");

        var aggregation = WindowPriceAggregation.Create(
            symbol,
            DateTimeOffset.Parse("2026-01-01T12:00:10.000Z"),
            tradeId: 1,
            tradePrice: 100m);

        aggregation.Id.Should().NotBeEmpty();
    }
}