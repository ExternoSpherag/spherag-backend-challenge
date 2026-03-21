using FluentAssertions;
using RealtimeMarketData.Domain.Aggregates.PriceWindow;
using Xunit;

namespace RealtimeMarketData.Domain.Tests.Aggregates;

public sealed class PriceChangeAlertRuleTests
{
    [Fact]
    public void ShouldTrigger_WithPlusSixPercent_ShouldReturnTrue()
    {
        var result = PriceChangeAlertRule.ShouldTrigger(
            previousAveragePrice: 100m,
            currentAveragePrice: 106m,
            thresholdPercent: 5m);

        result.Should().BeTrue();
    }

    [Fact]
    public void ShouldTrigger_WithMinusSixPercent_ShouldReturnTrue()
    {
        var result = PriceChangeAlertRule.ShouldTrigger(
            previousAveragePrice: 100m,
            currentAveragePrice: 94m,
            thresholdPercent: 5m);

        result.Should().BeTrue();
    }

    [Fact]
    public void ShouldTrigger_WithExactlyFivePercent_ShouldReturnFalse()
    {
        var result = PriceChangeAlertRule.ShouldTrigger(
            previousAveragePrice: 100m,
            currentAveragePrice: 105m,
            thresholdPercent: 5m);

        result.Should().BeFalse();
    }
}