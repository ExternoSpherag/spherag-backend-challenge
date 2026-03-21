using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RealtimeMarketData.Application.Features.Streaming;
using RealtimeMarketData.Application.Features.Streaming.Commands.IngestTradeTick;
using RealtimeMarketData.Domain.Aggregates.PriceWindow;
using RealtimeMarketData.Domain.ValueObjects;
using Xunit;

namespace RealtimeMarketData.Application.Tests.Streaming;

public sealed class IngestTradeTickCommandHandlerTests
{
    private static readonly DateTimeOffset WindowStart = DateTimeOffset.Parse("2026-01-01T12:00:10Z");
    private static readonly DateTimeOffset WindowEnd = DateTimeOffset.Parse("2026-01-01T12:00:15Z");

    private readonly Mock<ITradeWindowAggregator> _aggregatorMock = new();
    private readonly Mock<IPriceWindowRepository> _repositoryMock = new();
    private readonly Mock<IPriceAlertSettings> _priceAlertSettingsMock = new();
    private readonly IngestTradeTickCommandHandler _handler;

    public IngestTradeTickCommandHandlerTests()
    {
        _priceAlertSettingsMock.SetupGet(s => s.ThresholdPercent).Returns(5m);

        _handler = new IngestTradeTickCommandHandler(
            _aggregatorMock.Object,
            _repositoryMock.Object,
            _priceAlertSettingsMock.Object,
            NullLogger<IngestTradeTickCommandHandler>.Instance);
    }

    private static IngestTradeTickCommand BuildCommand() => new(
        Symbol: "BTCUSDT",
        Price: 67321.11m,
        Quantity: 0.5m,
        TradeTimestamp: DateTimeOffset.Parse("2026-01-01T12:00:10.100Z"),
        TradeId: 12345);

    private static TradeWindowAggregationSnapshot BuildSnapshot(bool isDuplicate = false) => new(
        AggregationId: Guid.NewGuid(),
        Symbol: "BTCUSDT",
        WindowStart: WindowStart,
        WindowEnd: WindowEnd,
        AveragePrice: 67321.11m,
        TradeCount: 1,
        IsDuplicate: isDuplicate);

    [Fact]
    public async Task Handle_WithNonDuplicateTick_AndNewWindow_ShouldAddAndSave()
    {
        var command = BuildCommand();
        var snapshot = BuildSnapshot(isDuplicate: false);

        _aggregatorMock
            .Setup(a => a.AddTrade(It.IsAny<Symbol>(), command.TradeId, command.Price, command.TradeTimestamp))
            .Returns(snapshot);

        _repositoryMock
            .Setup(r => r.GetBySymbolAndWindowStartAsync(snapshot.Symbol, snapshot.WindowStart, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PriceWindow?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<PriceWindow>(), It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(r => r.Update(It.IsAny<PriceWindow>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithNonDuplicateTick_AndExistingWindow_ShouldUpdateAndSave()
    {
        var command = BuildCommand();
        var snapshot = BuildSnapshot(isDuplicate: false);
        var existingWindow = PriceWindow.Create(
            snapshot.AggregationId,
            snapshot.Symbol,
            snapshot.WindowStart,
            snapshot.WindowEnd,
            averagePrice: 50000m,
            tradeCount: 1);

        _aggregatorMock
            .Setup(a => a.AddTrade(It.IsAny<Symbol>(), command.TradeId, command.Price, command.TradeTimestamp))
            .Returns(snapshot);

        _repositoryMock
            .Setup(r => r.GetBySymbolAndWindowStartAsync(snapshot.Symbol, snapshot.WindowStart, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingWindow);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _repositoryMock.Verify(r => r.Update(existingWindow), Times.Once);
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<PriceWindow>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithDuplicateTick_ShouldSkipPersistenceAndReturnSuccess()
    {
        var command = BuildCommand();
        var snapshot = BuildSnapshot(isDuplicate: true);

        _aggregatorMock
            .Setup(a => a.AddTrade(It.IsAny<Symbol>(), command.TradeId, command.Price, command.TradeTimestamp))
            .Returns(snapshot);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _repositoryMock.Verify(
            r => r.GetBySymbolAndWindowStartAsync(It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<PriceWindow>(), It.IsAny<CancellationToken>()), Times.Never);
        _repositoryMock.Verify(r => r.Update(It.IsAny<PriceWindow>()), Times.Never);
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithNonDuplicateTick_AndNoPreviousConsecutiveWindow_ShouldNotLogAlert()
    {
        var command = BuildCommand();
        var snapshot = BuildSnapshot(isDuplicate: false);

        _aggregatorMock
            .Setup(a => a.AddTrade(It.IsAny<Symbol>(), command.TradeId, command.Price, command.TradeTimestamp))
            .Returns(snapshot);

        _repositoryMock
            .Setup(r => r.GetBySymbolAndWindowEndAsync(snapshot.Symbol, snapshot.WindowStart, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PriceWindow?)null);

        _repositoryMock
            .Setup(r => r.GetBySymbolAndWindowStartAsync(snapshot.Symbol, snapshot.WindowStart, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PriceWindow?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        _repositoryMock.Verify(
            r => r.GetBySymbolAndWindowEndAsync(snapshot.Symbol, snapshot.WindowStart, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
