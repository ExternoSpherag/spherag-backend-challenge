using BinanceIngestionService.Application.Services;
using BinanceIngestionService.Domain.Entities;
using BinanceIngestionService.Domain.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace BinanceIngestionService.Application.Tests;

public class TradeBatchProcessorTests
{
    [Fact]
    public async Task ProcessBatchAsync_WhenBatchIsEmpty_DoesNotPublishAnything()
    {
        var publisher = Substitute.For<ITradeSummaryPublisher>();
        var sut = new TradeBatchProcessor(publisher, NullLogger<TradeBatchProcessor>.Instance);

        await sut.ProcessBatchAsync(
            [],
            new DateTimeOffset(2026, 03, 21, 10, 00, 00, TimeSpan.Zero),
            new DateTimeOffset(2026, 03, 21, 10, 00, 05, TimeSpan.Zero),
            CancellationToken.None);

        await publisher.DidNotReceiveWithAnyArgs().PublishAsync(default!, default);
    }

    [Fact]
    public async Task ProcessBatchAsync_GroupsTradesBySymbolAndUsesTheFixedWindowBounds()
    {
        var publishedSummaries = new List<TradeSummary>();
        var publisher = Substitute.For<ITradeSummaryPublisher>();
        publisher
            .PublishAsync(Arg.Any<TradeSummary>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask)
            .AndDoes(callInfo => publishedSummaries.Add(callInfo.Arg<TradeSummary>()));

        var sut = new TradeBatchProcessor(publisher, NullLogger<TradeBatchProcessor>.Instance);
        var windowStart = new DateTimeOffset(2026, 03, 21, 10, 00, 00, TimeSpan.Zero);
        var windowEnd = new DateTimeOffset(2026, 03, 21, 10, 00, 05, TimeSpan.Zero);
        var trades = new[]
        {
            new TradeRow
            {
                Stream = "ethusdt@trade",
                Symbol = "ethusdt",
                Price = 10m,
                Quantity = 2m,
                TradeTimeUtc = new DateTimeOffset(2026, 03, 21, 10, 00, 00, TimeSpan.Zero)
            },
            new TradeRow
            {
                Stream = "BTCUSDT@trade",
                Symbol = "BTCUSDT",
                Price = 20m,
                Quantity = 3m,
                TradeTimeUtc = new DateTimeOffset(2026, 03, 21, 10, 00, 01, TimeSpan.Zero)
            },
            new TradeRow
            {
                Stream = "btcusdt@trade",
                Symbol = "btcusdt",
                Price = 40m,
                Quantity = 1m,
                TradeTimeUtc = new DateTimeOffset(2026, 03, 21, 10, 00, 04, TimeSpan.Zero)
            }
        };

        await sut.ProcessBatchAsync(trades, windowStart, windowEnd, CancellationToken.None);

        await publisher.Received(2).PublishAsync(Arg.Any<TradeSummary>(), CancellationToken.None);
        Assert.Collection(
            publishedSummaries,
            btcSummary =>
            {
                Assert.Equal("BTCUSDT", btcSummary.Symbol);
                Assert.Equal(2, btcSummary.Count);
                Assert.Equal(30m, btcSummary.AveragePrice);
                Assert.Equal(4m, btcSummary.TotalQuantity);
                Assert.Equal(windowEnd, btcSummary.TimeUtc);
                Assert.Equal(windowStart, btcSummary.WindowStart);
                Assert.Equal(windowEnd, btcSummary.WindowEnd);
            },
            ethSummary =>
            {
                Assert.Equal("ethusdt", ethSummary.Symbol);
                Assert.Equal(1, ethSummary.Count);
                Assert.Equal(10m, ethSummary.AveragePrice);
                Assert.Equal(2m, ethSummary.TotalQuantity);
                Assert.Equal(windowEnd, ethSummary.TimeUtc);
                Assert.Equal(windowStart, ethSummary.WindowStart);
                Assert.Equal(windowEnd, ethSummary.WindowEnd);
            });
    }
}
