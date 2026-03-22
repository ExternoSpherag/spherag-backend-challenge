using ApiLectura.Application.Common;
using ApiLectura.Domain.Interfaces;

namespace ApiLectura.Application.UseCases.PosicionesAgregadas.GetPosicionesAgregadasBySymbol;

public class GetPosicionesAgregadasBySymbolHandler(IPosicionAgregadaRepository repository)
{
    public async Task<IEnumerable<GetPosicionesAgregadasBySymbolItem>> HandleAsync(
        string symbol,
        GetPosicionesAgregadasBySymbolQuery query,
        CancellationToken cancellationToken)
    {
        PagingRules.Normalize(query);

        var result = await repository.GetPosicionAgregadasBySymbolAsync(query.Page, query.PageSize, symbol, cancellationToken);

        return [.. result.Items
            .Select(x => new GetPosicionesAgregadasBySymbolItem(
                x.TimeUtc,
                x.Symbol,
                x.Count,
                x.AveragePrice,
                x.TotalQuantity,
                x.WindowStart,
                x.WindowEnd))];
    }
}
