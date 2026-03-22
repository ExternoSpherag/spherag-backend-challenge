using ApiLectura.Application.UseCases.PosicionesAgregadas.GetPosicionesAgregadasBySymbol;
using ApiLectura.Domain.Interfaces;
using ApiLectura.Domain.Models;
using NSubstitute;
using Xunit;

namespace ApiLectura.Tests;

public class GetPosicionesAgregadasBySymbolHandlerTests
{
    [Fact]
    public async Task HandleAsync_ReturnsMappedItems_ForGivenSymbol()
    {
        var repository = Substitute.For<IPosicionAgregadaRepository>();

        var now = DateTimeOffset.UtcNow;
        var items = new List<PosicionAgregada>
        {
            new()
            {
                Symbol = "SOLUSDT",
                Count = 2,
                AveragePrice = 180m,
                TotalQuantity = 50m,
                TimeUtc = now,
                WindowStart = now.AddMinutes(-1),
                WindowEnd = now
            }
        };

        var paginated = new PaginatedResponse<PosicionAgregada>
        {
            Page = 1,
            SizePage = 10,
            TotalItems = 1,
            TotalPages = 1,
            HasNextPage = false,
            HasPreviousPage = false,
            Items = items
        };

        repository
            .GetPosicionAgregadasBySymbolAsync(1, 10, "SOLUSDT", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(paginated));

        var handler = new GetPosicionesAgregadasBySymbolHandler(repository);
        var query = new GetPosicionesAgregadasBySymbolQuery { Page = 1, PageSize = 10 };

        var result = await handler.HandleAsync("SOLUSDT", query, CancellationToken.None);

        var item = Assert.Single(result);
        Assert.Equal("SOLUSDT", item.Symbol);
        Assert.Equal(2, item.Count);
        Assert.Equal(180m, item.AveragePrice);
        Assert.Equal(50m, item.TotalQuantity);

        await repository.Received(1)
            .GetPosicionAgregadasBySymbolAsync(1, 10, "SOLUSDT", Arg.Any<CancellationToken>());
    }
}
