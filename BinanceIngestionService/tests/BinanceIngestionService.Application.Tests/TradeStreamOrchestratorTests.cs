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
            Options.Create(new BatchingOptions { WindowSeconds = 5, AllowedLatenessSeconds = 0 }),
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
            Options.Create(new BatchingOptions { WindowSeconds = 5, AllowedLatenessSeconds = 0 }),
            NullLogger<TradeStreamOrchestrator>.Instance);

        await sut.RunAsync(CancellationToken.None);

        await publisher.DidNotReceiveWithAnyArgs().PublishAsync(default!, default);
    }

    [Fact]
    public async Task RunAsync_WhenTradesArriveOutOfOrderWithinAllowedLateness_ProcessesThemWithoutDiscarding()
    {
        var streamClient = Substitute.For<ITradeStreamClient>();
        var parser = Substitute.For<ITradeMessageParser>();
        var publisher = Substitute.For<ITradeSummaryPublisher>();
        var logger = new TestLogger<TradeStreamOrchestrator>();
        var clock = Substitute.For<IClock>();
        clock.UtcNow.Returns(new DateTimeOffset(2026, 03, 21, 10, 00, 20, TimeSpan.Zero));

        var laterTrade = new TradeRow
        {
            Stream = "btcusdt@trade",
            Symbol = "BTCUSDT",
            Price = 110m,
            Quantity = 1m,
            TradeTimeUtc = new DateTimeOffset(2026, 03, 21, 10, 00, 06, TimeSpan.Zero)
        };

        var earlierTrade = new TradeRow
        {
            Stream = "btcusdt@trade",
            Symbol = "BTCUSDT",
            Price = 100m,
            Quantity = 1m,
            TradeTimeUtc = new DateTimeOffset(2026, 03, 21, 10, 00, 02, TimeSpan.Zero)
        };

        streamClient.ReadMessagesAsync(Arg.Any<CancellationToken>())
            .Returns(ReadMessages("later", "earlier"));

        parser.TryParse("later", out Arg.Any<TradeRow?>())
            .Returns(callInfo =>
            {
                callInfo[1] = laterTrade;
                return true;
            });

        parser.TryParse("earlier", out Arg.Any<TradeRow?>())
            .Returns(callInfo =>
            {
                callInfo[1] = earlierTrade;
                return true;
            });

        var batchProcessor = new TradeBatchProcessor(publisher, NullLogger<TradeBatchProcessor>.Instance);
        var sut = new TradeStreamOrchestrator(
            streamClient,
            parser,
            batchProcessor,
            clock,
            Options.Create(new BatchingOptions { WindowSeconds = 5, AllowedLatenessSeconds = 30 }),
            logger);

        await sut.RunAsync(CancellationToken.None);

        await publisher.Received(2).PublishAsync(Arg.Any<TradeSummary>(), CancellationToken.None);
        Assert.Contains(logger.Messages, message => message.Contains("tardio/fuera de orden", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task RunAsync_WhenAWindowHasNoTrades_LogsIt()
    {
        var streamClient = Substitute.For<ITradeStreamClient>();
        var parser = Substitute.For<ITradeMessageParser>();
        var publisher = Substitute.For<ITradeSummaryPublisher>();
        var logger = new TestLogger<TradeStreamOrchestrator>();
        var clock = Substitute.For<IClock>();
        clock.UtcNow.Returns(new DateTimeOffset(2026, 03, 21, 10, 00, 20, TimeSpan.Zero));

        var firstTrade = new TradeRow
        {
            Stream = "btcusdt@trade",
            Symbol = "BTCUSDT",
            Price = 100m,
            Quantity = 1m,
            TradeTimeUtc = new DateTimeOffset(2026, 03, 21, 10, 00, 00, TimeSpan.Zero)
        };

        var secondTrade = new TradeRow
        {
            Stream = "btcusdt@trade",
            Symbol = "BTCUSDT",
            Price = 110m,
            Quantity = 1m,
            TradeTimeUtc = new DateTimeOffset(2026, 03, 21, 10, 00, 10, TimeSpan.Zero)
        };

        streamClient.ReadMessagesAsync(Arg.Any<CancellationToken>())
            .Returns(ReadMessages("first", "second"));

        parser.TryParse("first", out Arg.Any<TradeRow?>())
            .Returns(callInfo =>
            {
                callInfo[1] = firstTrade;
                return true;
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
            Options.Create(new BatchingOptions { WindowSeconds = 5, AllowedLatenessSeconds = 0 }),
            logger);

        await sut.RunAsync(CancellationToken.None);

        Assert.Contains(logger.Messages, message => message.Contains("No hubo trades", StringComparison.OrdinalIgnoreCase));
    }

    private static async IAsyncEnumerable<string> ReadMessages(params string[] messages)
    {
        foreach (var message in messages)
        {
            yield return message;
            await Task.Yield();
        }
    }

    private sealed class TestLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = [];

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
        }

        private sealed class NullScope : IDisposable
        {
            public static NullScope Instance { get; } = new();

            public void Dispose()
            {
            }
        }
    }
}
