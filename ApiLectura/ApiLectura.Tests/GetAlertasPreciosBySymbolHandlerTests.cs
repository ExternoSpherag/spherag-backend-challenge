using ApiLectura.Application.UseCases.AlertasPrecios.GetAlertasPreciosBySymbol;
using ApiLectura.Domain.Entities;
using ApiLectura.Domain.Interfaces;
using ApiLectura.Domain.Models;
using NSubstitute;
using Xunit;

namespace ApiLectura.Tests;

public class GetAlertasPreciosBySymbolHandlerTests
{
    [Fact]
    public async Task HandleAsync_ReturnsMappedItems_ForGivenSymbol()
    {
        var repository = Substitute.For<IAlertaPreciosRepository>();

        var items = new List<AlertaPrecios>
        {
            new AlertaPrecios { CreatedAt = DateTimeOffset.UtcNow, Symbol = "ETHUSDT", PreviousTime = DateTimeOffset.UtcNow.AddMinutes(-5), CurrentTime = DateTimeOffset.UtcNow, PreviousAverage = 200m, CurrentAverage = 220m, Percentage = 10m, Direction = "UP" }
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

        repository.GetAlertaPreciosBySymbolAsync(Arg.Any<int>(), Arg.Any<int>(), "ETHUSDT", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(paginated));

        var handler = new GetAlertasPreciosBySymbolHandler(repository);

        var query = new GetAlertasPreciosBySymbolQuery { Page = 1, PageSize = 10 };

        var result = await handler.HandleAsync("ETHUSDT", query, CancellationToken.None);

        Assert.Single(result);
        var first = result.First();
        Assert.Equal("ETHUSDT", first.Symbol);
        Assert.Equal(200m, first.PreviousAverage);
        Assert.Equal(220m, first.CurrentAverage);
    }
}
