namespace ApiLectura.Application.UseCases.Prices.GetPrices;

public sealed record GetPricesItem(
    string Symbol,
    DateTimeOffset WindowStart,
    DateTimeOffset WindowEnd,
    decimal AveragePrice);
