using FluentAssertions;
using RealtimeMarketData.Infrastructure.Streaming.Common;
using Xunit;

namespace RealtimeMarketData.Application.Tests.Streaming;

public sealed class BoundedBackoffPolicyTests
{
    private static readonly TimeSpan Base = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan Max  = TimeSpan.FromSeconds(30);

    [Theory]
    [InlineData(1,  1)]
    [InlineData(2,  2)]
    [InlineData(3,  4)]
    [InlineData(4,  8)]
    [InlineData(5,  16)]
    [InlineData(6,  30)]
    [InlineData(10, 30)]
    public void Calculate_ShouldReturnExponentialDelayBoundedByMax(int attempt, double expectedSeconds)
    {
        var result = BoundedBackoffPolicy.Calculate(attempt, Base, Max);

        result.Should().Be(TimeSpan.FromSeconds(expectedSeconds));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Calculate_WithNonPositiveAttempt_ShouldReturnBaseDelay(int attempt)
    {
        var result = BoundedBackoffPolicy.Calculate(attempt, Base, Max);

        result.Should().Be(Base);
    }

    [Fact]
    public void Calculate_WhenMaxSmallerThanBase_ShouldReturnMaxForFirstAttempt()
    {
        var tinyMax = TimeSpan.FromMilliseconds(200);

        var result = BoundedBackoffPolicy.Calculate(1, Base, tinyMax);

        result.Should().Be(tinyMax);
    }
}