using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using AlertConsumer.Application.Abstractions;
using AlertConsumer.Application.Services;
using AlertConsumer.Domain.Entities;
using AlertConsumer.Domain.Enums;
using AlertConsumer.Domain.Services;
using Xunit;

namespace AlertConsumer.Application.Tests.Services;

public class TradeSummaryProcessorTests
{
    private readonly ITradeSummarySnapshotRepository _snapshotRepository = Substitute.For<ITradeSummarySnapshotRepository>();
    private readonly IPriceAlertRepository _priceAlertRepository = Substitute.For<IPriceAlertRepository>();
    private readonly PriceAlertEvaluator _evaluator = new(5m);
    private readonly NullLogger<TradeSummaryProcessor> _logger = NullLogger<TradeSummaryProcessor>.Instance;

    [Fact]
    public async Task ProcessAsync_WhenFirstMessage_SavesSnapshotAndDoesNotPersistAlert()
    {
        var trade = CreateTradeSummary("BTCUSDT", 100m, new DateTimeOffset(2026, 3, 21, 10, 0, 0, TimeSpan.Zero));
        _snapshotRepository.GetLastBySymbolAsync(trade.Symbol, Arg.Any<CancellationToken>())
            .Returns((TradeSummary?)null);

        var sut = CreateSut();

        await sut.ProcessAsync(trade);

        await _priceAlertRepository.DidNotReceive()
            .AddAsync(Arg.Any<PriceAlert>(), Arg.Any<CancellationToken>());
        await _snapshotRepository.Received(1)
            .SaveAsync(trade, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_WhenThresholdExceeded_PersistsAlertAndSnapshot()
    {
        var previousTrade = CreateTradeSummary("BTCUSDT", 100m, new DateTimeOffset(2026, 3, 21, 10, 0, 0, TimeSpan.Zero));
        var currentTrade = CreateTradeSummary("BTCUSDT", 106m, new DateTimeOffset(2026, 3, 21, 10, 1, 0, TimeSpan.Zero));

        _snapshotRepository.GetLastBySymbolAsync(currentTrade.Symbol, Arg.Any<CancellationToken>())
            .Returns(previousTrade);

        var sut = CreateSut();

        await sut.ProcessAsync(currentTrade);

        await _priceAlertRepository.Received(1).AddAsync(
            Arg.Is<PriceAlert>(alert =>
                alert.Symbol == currentTrade.Symbol &&
                alert.PreviousAveragePrice == previousTrade.AveragePrice &&
                alert.CurrentAveragePrice == currentTrade.AveragePrice &&
                alert.Direction == PriceDirection.Up &&
                alert.PercentageChange == 6m),
            Arg.Any<CancellationToken>());

        await _snapshotRepository.Received(1)
            .SaveAsync(currentTrade, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_WhenThresholdNotExceeded_DoesNotPersistAlert()
    {
        var previousTrade = CreateTradeSummary("ETHUSDT", 100m, new DateTimeOffset(2026, 3, 21, 10, 0, 0, TimeSpan.Zero));
        var currentTrade = CreateTradeSummary("ETHUSDT", 104m, new DateTimeOffset(2026, 3, 21, 10, 1, 0, TimeSpan.Zero));

        _snapshotRepository.GetLastBySymbolAsync(currentTrade.Symbol, Arg.Any<CancellationToken>())
            .Returns(previousTrade);

        var sut = CreateSut();

        await sut.ProcessAsync(currentTrade);

        await _priceAlertRepository.DidNotReceive()
            .AddAsync(Arg.Any<PriceAlert>(), Arg.Any<CancellationToken>());
        await _snapshotRepository.Received(1)
            .SaveAsync(currentTrade, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_WhenPriceDropsOverThreshold_PersistsDownAlert()
    {
        var previousTrade = CreateTradeSummary("SOLUSDT", 100m, new DateTimeOffset(2026, 3, 21, 10, 0, 0, TimeSpan.Zero));
        var currentTrade = CreateTradeSummary("SOLUSDT", 90m, new DateTimeOffset(2026, 3, 21, 10, 1, 0, TimeSpan.Zero));

        _snapshotRepository.GetLastBySymbolAsync(currentTrade.Symbol, Arg.Any<CancellationToken>())
            .Returns(previousTrade);

        var sut = CreateSut();

        await sut.ProcessAsync(currentTrade);

        await _priceAlertRepository.Received(1).AddAsync(
            Arg.Is<PriceAlert>(alert =>
                alert.Symbol == currentTrade.Symbol &&
                alert.Direction == PriceDirection.Down &&
                alert.PercentageChange == 10m),
            Arg.Any<CancellationToken>());
    }

    private TradeSummaryProcessor CreateSut() =>
        new(_snapshotRepository, _priceAlertRepository, _evaluator, _logger);

    private static TradeSummary CreateTradeSummary(string symbol, decimal averagePrice, DateTimeOffset timeUtc) =>
        new()
        {
            Symbol = symbol,
            Count = 10,
            AveragePrice = averagePrice,
            TotalQuantity = 2m,
            TimeUtc = timeUtc,
            WindowStart = timeUtc.AddMinutes(-1),
            WindowEnd = timeUtc
        };
}

