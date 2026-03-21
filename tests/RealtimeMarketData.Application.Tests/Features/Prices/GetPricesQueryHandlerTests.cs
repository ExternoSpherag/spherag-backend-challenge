using FluentAssertions;
using Moq;
using RealtimeMarketData.Application.Features.Prices.Queries.GetPrices;
using RealtimeMarketData.Domain.Aggregates.PriceWindow;
using Xunit;

namespace RealtimeMarketData.Application.Tests.Features.Prices;

public sealed class GetPricesQueryHandlerTests
{
    private static readonly DateTimeOffset WindowStart = DateTimeOffset.Parse("2026-01-01T12:00:00Z");
    private static readonly DateTimeOffset WindowEnd = DateTimeOffset.Parse("2026-01-01T12:00:05Z");

    private readonly Mock<IPriceWindowRepository> _repositoryMock = new();
    private readonly GetPricesQueryHandler _handler;

    public GetPricesQueryHandlerTests()
    {
        _handler = new GetPricesQueryHandler(_repositoryMock.Object);
    }

    private static PriceWindow BuildWindow(string symbol = "BTCUSDT") =>
        PriceWindow.Create(Guid.NewGuid(), symbol, WindowStart, WindowEnd, 67321.11m, 10);

    [Fact]
    public async Task Handle_NoFilters_ReturnsAllWindows()
    {
        IReadOnlyList<PriceWindow> windows = [BuildWindow("BTCUSDT"), BuildWindow("ETHUSDT")];
        _repositoryMock
            .Setup(r => r.GetFilteredAsync(null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(windows);

        var result = await _handler.Handle(new GetPricesQuery(null, null, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_WithSymbolFilter_ReturnsMatchingWindows()
    {
        IReadOnlyList<PriceWindow> windows = [BuildWindow("BTCUSDT")];
        _repositoryMock
            .Setup(r => r.GetFilteredAsync("BTCUSDT", null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(windows);

        var result = await _handler.Handle(new GetPricesQuery("BTCUSDT", null, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Should().HaveCount(1);
        result.Value![0].Symbol.Should().Be("BTCUSDT");
    }

    [Fact]
    public async Task Handle_WithFromToRange_ReturnsFilteredWindows()
    {
        var from = DateTimeOffset.Parse("2026-01-01T12:00:00Z");
        var to = DateTimeOffset.Parse("2026-01-01T12:05:00Z");
        IReadOnlyList<PriceWindow> windows = [BuildWindow()];
        _repositoryMock
            .Setup(r => r.GetFilteredAsync(null, from, to, It.IsAny<CancellationToken>()))
            .ReturnsAsync(windows);

        var result = await _handler.Handle(new GetPricesQuery(null, from, to), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_MapsResponseFieldsCorrectly()
    {
        var window = BuildWindow("BTCUSDT");
        IReadOnlyList<PriceWindow> windows = [window];
        _repositoryMock
            .Setup(r => r.GetFilteredAsync(null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(windows);

        var result = await _handler.Handle(new GetPricesQuery(null, null, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var item = result.Value![0];
        item.Symbol.Should().Be(window.Symbol);
        item.WindowStart.Should().Be(window.WindowStart);
        item.WindowEnd.Should().Be(window.WindowEnd);
        item.AveragePrice.Should().Be(window.AveragePrice);
    }
}