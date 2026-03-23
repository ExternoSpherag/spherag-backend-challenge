using ApiLectura.Application.UseCases.Prices.GetPrices;
using ApiLectura.Contracts.Response;

namespace ApiLectura.Mappers;

public static class PriceDtoMapper
{
    public static PriceResponse ToResponse(GetPricesItem item) =>
        new()
        {
            Symbol = item.Symbol,
            WindowStart = item.WindowStart,
            WindowEnd = item.WindowEnd,
            AveragePrice = item.AveragePrice
        };
}
