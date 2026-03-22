using ApiLectura.Application.Common;
using ApiLectura.Domain.Interfaces;

namespace ApiLectura.Application.UseCases.PosicionesAgregadas.GetAllPosicionesAgregadas;

public class GetAllPosicionesAgregadasHandler(IPosicionAgregadaRepository repository)
{
    public async Task<IEnumerable<GetAllPosicionesAgregadasItem>> HandleAsync(
        GetAllPosicionesAgregadasQuery query,
        CancellationToken cancellationToken)
    {
        PagingRules.Normalize(query);

        var result = await repository.GetAllAsync(query.Page, query.PageSize, cancellationToken);

        return [.. result.Items
            .Select(x => new GetAllPosicionesAgregadasItem(
                x.TimeUtc,
                x.Symbol,
                x.Count,
                x.AveragePrice,
                x.TotalQuantity,
                x.WindowStart,
                x.WindowEnd))];
    }
}
