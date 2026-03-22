using ApiLectura.Application.UseCases.AlertasPrecios.GetAlertasPreciosByDirection;
using ApiLectura.Domain.Entities;
using ApiLectura.Domain.Interfaces;
using ApiLectura.Domain.Models;
using NSubstitute;
using Xunit;

namespace ApiLectura.Tests;

public class GetAlertasPreciosByDirectionHandlerTests
{
    [Fact]
    public async Task HandleAsync_ReturnsMappedItems_And_ClampsPageSizeToMaximum()
    {
        var repository = Substitute.For<IAlertaPreciosRepository>();

        var items = new List<AlertaPrecios>
        {
            new()
            {
                CreatedAt = DateTimeOffset.UtcNow,
                Symbol = "BTCUSDT",
                PreviousTime = DateTimeOffset.UtcNow.AddMinutes(-15),
                CurrentTime = DateTimeOffset.UtcNow,
                PreviousAverage = 99m,
                CurrentAverage = 105m,
                Percentage = 6m,
                Direction = "UP"
            }
        };

        var paginated = new PaginatedResponse<AlertaPrecios>
        {
            Page = 2,
            SizePage = 100,
            TotalItems = 1,
            TotalPages = 1,
            HasNextPage = false,
            HasPreviousPage = true,
            Items = items
        };

        repository
            .GetAlertaPreciosByDirectionAsync(2, 100, "UP", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(paginated));

        var handler = new GetAlertasPreciosByDirectionHandler(repository);
        var query = new GetAlertasPreciosByDirectionQuery { Page = 2, PageSize = 150 };

        var result = await handler.HandleAsync("UP", query, CancellationToken.None);

        var item = Assert.Single(result);
        Assert.Equal("BTCUSDT", item.Symbol);
        Assert.Equal(99m, item.PreviousAverage);
        Assert.Equal(105m, item.CurrentAverage);

        await repository.Received(1)
            .GetAlertaPreciosByDirectionAsync(2, 100, "UP", Arg.Any<CancellationToken>());
    }
}
