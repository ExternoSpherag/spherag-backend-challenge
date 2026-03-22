using ApiLectura.Application.UseCases.PosicionesAgregadas.GetAllPosicionesAgregadas;
using ApiLectura.Domain.Interfaces;
using ApiLectura.Domain.Models;
using NSubstitute;
using Xunit;

namespace ApiLectura.Tests;

public class GetAllPosicionesAgregadasHandlerTests
{
    [Fact]
    public async Task HandleAsync_ReturnsMappedItems_And_AppliesDefaultPaging()
    {
        var repository = Substitute.For<IPosicionAgregadaRepository>();

        var now = DateTimeOffset.UtcNow;
        var items = new List<PosicionAgregada>
        {
            new()
            {
                Symbol = "ETHUSDT",
                Count = 3,
                AveragePrice = 2500m,
                TotalQuantity = 12m,
                TimeUtc = now,
                WindowStart = now.AddMinutes(-5),
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
            .GetAllAsync(1, 10, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(paginated));

        var handler = new GetAllPosicionesAgregadasHandler(repository);
        var query = new GetAllPosicionesAgregadasQuery { Page = 0, PageSize = 0 };

        var result = await handler.HandleAsync(query, CancellationToken.None);

        var item = Assert.Single(result);
        Assert.Equal("ETHUSDT", item.Symbol);
        Assert.Equal(3, item.Count);
        Assert.Equal(2500m, item.AveragePrice);
        Assert.Equal(12m, item.TotalQuantity);

        await repository.Received(1)
            .GetAllAsync(1, 10, Arg.Any<CancellationToken>());
    }
}
