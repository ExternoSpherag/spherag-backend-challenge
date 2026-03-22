using BinanceIngestionService.Application.Configuration;
using BinanceIngestionService.Application.Services;
using BinanceIngestionService.Domain.Abstractions;
using BinanceIngestionService.Domain.Entities;
using BinanceIngestionService.Domain.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace BinanceIngestionService.Application.Tests;

public class TradeStreamOrchestratorTests
{
    [Fact]
    public async Task RunAsync_FlushesTradesUsingWallClockAlignedWindows()
    {
        var streamClient = Substitute.For<ITradeStreamClient>();
        var parser = Substitute.For<ITradeMessageParser>();
        var publisher = Substitute.For<ITradeSummaryPublisher>();
        var clock = Substitute.For<IClock>();
        clock.UtcNow.Returns(new DateTimeOffset(2026, 03, 21, 10, 00, 10, TimeSpan.Zero));

        var firstTrade = new TradeRow
        {
            Stream = "btcusdt@trade",
            Symbol = "BTCUSDT",
            Price = 100m,
            Quantity = 1.5m,
            TradeTimeUtc = new DateTimeOffset(2026, 03, 21, 10, 00, 02, TimeSpan.Zero)
        };

        var secondTrade = new TradeRow
        {
            Stream = "btcusdt@trade",
            Symbol = "BTCUSDT",
            Price = 110m,
            Quantity = 2.5m,
            TradeTimeUtc = new DateTimeOffset(2026, 03, 21, 10, 00, 06, TimeSpan.Zero)
        };

        streamClient.ReadMessagesAsync(Arg.Any<CancellationToken>())
            .Returns(ReadMessages("first", "invalid", "second"));

        parser.TryParse("first", out Arg.Any<TradeRow?>())
            .Returns(callInfo =>
            {
                callInfo[1] = firstTrade;
                return true;
            });

        parser.TryParse("invalid", out Arg.Any<TradeRow?>())
            .Returns(callInfo =>
            {
                callInfo[1] = null;
                return false;
            });

        parser.TryParse("second", out Arg.Any<TradeRow?>())
            .Returns(callInfo =>
            {
                callInfo[1] = secondTrade;
                return true;
            });

        var batchProcessor = new TradeBatchProcessor(publisher, NullLogger<TradeBatchProcessor>.Instance);
        var sut = new TradeStreamOrchestrator(
            streamClient,
            parser,
            batchProcessor,
            clock,
            Options.Create(new BatchingOptions { WindowSeconds = 5 }),
            NullLogger<TradeStreamOrchestrator>.Instance);

        await sut.RunAsync(CancellationToken.None);

        await publisher.Received(2).PublishAsync(Arg.Any<TradeSummary>(), CancellationToken.None);
        await publisher.Received(1).PublishAsync(
            Arg.Is<TradeSummary>(summary =>
                summary.Symbol == "BTCUSDT" &&
                summary.Count == 1 &&
                summary.TimeUtc == new DateTimeOffset(2026, 03, 21, 10, 00, 05, TimeSpan.Zero) &&
                summary.WindowStart == new DateTimeOffset(2026, 03, 21, 10, 00, 00, TimeSpan.Zero) &&
                summary.WindowEnd == new DateTimeOffset(2026, 03, 21, 10, 00, 05, TimeSpan.Zero)),
            CancellationToken.None);

        await publisher.Received(1).PublishAsync(
            Arg.Is<TradeSummary>(summary =>
                summary.Symbol == "BTCUSDT" &&
                summary.Count == 1 &&
                summary.TimeUtc == new DateTimeOffset(2026, 03, 21, 10, 00, 10, TimeSpan.Zero) &&
                summary.WindowStart == new DateTimeOffset(2026, 03, 21, 10, 00, 05, TimeSpan.Zero) &&
                summary.WindowEnd == new DateTimeOffset(2026, 03, 21, 10, 00, 10, TimeSpan.Zero)),
            CancellationToken.None);
    }

    [Fact]
    public async Task RunAsync_WhenNoValidTradesAreParsed_DoesNotPublishAnything()
    {
        var streamClient = Substitute.For<ITradeStreamClient>();
        var parser = Substitute.For<ITradeMessageParser>();
        var publisher = Substitute.For<ITradeSummaryPublisher>();
        var clock = Substitute.For<IClock>();

        streamClient.ReadMessagesAsync(Arg.Any<CancellationToken>())
            .Returns(ReadMessages("invalid"));

        parser.TryParse("invalid", out Arg.Any<TradeRow?>())
            .Returns(callInfo =>
            {
                callInfo[1] = null;
                return false;
            });

        var batchProcessor = new TradeBatchProcessor(publisher, NullLogger<TradeBatchProcessor>.Instance);
        var sut = new TradeStreamOrchestrator(
            streamClient,
            parser,
            batchProcessor,
            clock,
            Options.Create(new BatchingOptions { WindowSeconds = 5 }),
            NullLogger<TradeStreamOrchestrator>.Instance);

        await sut.RunAsync(CancellationToken.None);

        await publisher.DidNotReceiveWithAnyArgs().PublishAsync(default!, default);
    }

    private static async IAsyncEnumerable<string> ReadMessages(params string[] messages)
    {
        foreach (var message in messages)
        {
            yield return message;
            await Task.Yield();
        }
    }
}
