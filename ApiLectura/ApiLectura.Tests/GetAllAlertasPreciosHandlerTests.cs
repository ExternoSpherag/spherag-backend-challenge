using ApiLectura.Application.UseCases.AlertasPrecios.GetAllAlertasPrecios;
using ApiLectura.Domain.Entities;
using ApiLectura.Domain.Interfaces;
using ApiLectura.Domain.Models;
using NSubstitute;
using Xunit;

namespace ApiLectura.Tests;

public class GetAllAlertasPreciosHandlerTests
{
    [Fact]
    public async Task HandleAsync_ReturnsMappedItems_And_RespectsPagingBounds()
    {
        var repository = Substitute.For<IAlertaPreciosRepository>();

        var items = new List<AlertaPrecios>
        {
            new AlertaPrecios { CreatedAt = DateTimeOffset.UtcNow, Symbol = "BTCUSDT", PreviousTime = DateTimeOffset.UtcNow.AddMinutes(-10), CurrentTime = DateTimeOffset.UtcNow, PreviousAverage = 100m, CurrentAverage = 110m, Percentage = 10m, Direction = "UP" }
        };

        var paginated = new PaginatedResponse<AlertaPrecios>
        {
            Page = 1,
            SizePage = 10,
            TotalItems = 1,
            TotalPages = 1,
            HasNextPage = false,
            HasPreviousPage = false,
            Items = items
        };

        repository.GetAllAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(paginated));

        var handler = new GetAllAlertasPreciosHandler(repository);

        var query = new GetAllAlertasPreciosQuery { Page = 0, PageSize = 0 };

        var result = await handler.HandleAsync(query, CancellationToken.None);

        Assert.Single(result);
        var first = result.First();
        Assert.Equal("BTCUSDT", first.Symbol);
        Assert.Equal(100m, first.PreviousAverage);
        Assert.Equal(110m, first.CurrentAverage);
    }
}
