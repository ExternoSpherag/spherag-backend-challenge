using ApiLectura.Application.UseCases.Prices.GetPrices;
using ApiLectura.Domain.Interfaces;
using ApiLectura.Domain.Models;
using NSubstitute;
using Xunit;

namespace ApiLectura.Tests;

public class GetPricesHandlerTests
{
    [Fact]
    public async Task HandleAsync_ReturnsMappedItems_WithFilters()
    {
        var repository = Substitute.For<IPosicionAgregadaRepository>();
        var from = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var to = from.AddSeconds(10);

        repository.GetPricesAsync(1, 10, "BTCUSDT", from, to, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new PaginatedResponse<PosicionAgregada>
            {
                Page = 1,
                SizePage = 10,
                TotalItems = 1,
                TotalPages = 1,
                HasNextPage = false,
                HasPreviousPage = false,
                Items =
                [
                    new PosicionAgregada
                    {
                        Symbol = "BTCUSDT",
                        Count = 3,
                        AveragePrice = 67321.11m,
                        TotalQuantity = 1.2m,
                        TimeUtc = to,
                        WindowStart = from,
                        WindowEnd = from.AddSeconds(5)
                    }
                ]
            }));

        var handler = new GetPricesHandler(repository);

        var result = await handler.HandleAsync(new GetPricesQuery
        {
            Symbol = "BTCUSDT",
            From = from,
            To = to,
            Page = 0,
            PageSize = 0
        }, CancellationToken.None);

        var item = Assert.Single(result);
        Assert.Equal("BTCUSDT", item.Symbol);
        Assert.Equal(from, item.WindowStart);
        Assert.Equal(from.AddSeconds(5), item.WindowEnd);
        Assert.Equal(67321.11m, item.AveragePrice);

        await repository.Received(1)
            .GetPricesAsync(1, 10, "BTCUSDT", from, to, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Throws_WhenFromIsGreaterThanTo()
    {
        var repository = Substitute.For<IPosicionAgregadaRepository>();
        var from = new DateTimeOffset(2026, 1, 1, 12, 0, 10, TimeSpan.Zero);
        var to = new DateTimeOffset(2026, 1, 1, 12, 0, 5, TimeSpan.Zero);
        var handler = new GetPricesHandler(repository);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            handler.HandleAsync(new GetPricesQuery
            {
                Symbol = "BTCUSDT",
                From = from,
                To = to
            }, CancellationToken.None));
    }
}
