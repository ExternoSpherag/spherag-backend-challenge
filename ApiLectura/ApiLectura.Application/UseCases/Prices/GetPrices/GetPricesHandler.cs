using ApiLectura.Application.Common;
using ApiLectura.Domain.Interfaces;

namespace ApiLectura.Application.UseCases.Prices.GetPrices;

public class GetPricesHandler(IPosicionAgregadaRepository repository)
{
    public async Task<IReadOnlyList<GetPricesItem>> HandleAsync(
        GetPricesQuery query,
        CancellationToken cancellationToken)
    {
        PagingRules.Normalize(query);

        if (query.From.HasValue && query.To.HasValue && query.From > query.To)
        {
            throw new ArgumentException("'from' must be less than or equal to 'to'.", nameof(query));
        }

        var result = await repository.GetPricesAsync(
            query.Page,
            query.PageSize,
            query.Symbol,
            query.From,
            query.To,
            cancellationToken);

        return [.. result.Items.Select(x => new GetPricesItem(
            x.Symbol,
            x.WindowStart,
            x.WindowEnd,
            x.AveragePrice))];
    }
}
