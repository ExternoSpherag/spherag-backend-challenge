using ApiLectura.Application.Common;
using ApiLectura.Validations;

namespace ApiLectura.Application.UseCases.Prices.GetPrices;

public sealed record GetPricesQuery : PagedQuery
{
    [AllowedValues("BTCUSDT", "ETHUSDT", "DOGEUSDT")]
    public string? Symbol { get; init; }
    public DateTimeOffset? From { get; init; }
    public DateTimeOffset? To { get; init; }
}
