using FluentAssertions;
using RealtimeMarketData.Domain.ValueObjects;
using Xunit;

namespace RealtimeMarketData.Domain.Tests.ValueObjects;

public sealed class FiveSecondWindowTests
{
    [Theory]
    [InlineData("2026-01-01T12:00:04.999Z", "2026-01-01T12:00:00Z", "2026-01-01T12:00:05Z")]
    [InlineData("2026-01-01T12:00:05.000Z", "2026-01-01T12:00:05Z", "2026-01-01T12:00:10Z")]
    [InlineData("2026-01-01T12:00:10.123Z", "2026-01-01T12:00:10Z", "2026-01-01T12:00:15Z")]
    public void FromTradeTimestamp_ShouldAlignToExpectedWindow(
        string tradeTimestamp,
        string expectedStart,
        string expectedEnd)
    {
        var result = FiveSecondWindow.FromTradeTimestamp(DateTimeOffset.Parse(tradeTimestamp));

        result.Start.Should().Be(DateTimeOffset.Parse(expectedStart));
        result.End.Should().Be(DateTimeOffset.Parse(expectedEnd));
    }
}