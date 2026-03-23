using ApiLectura.Application.Common;
using ApiLectura.Domain.Interfaces;

namespace ApiLectura.Application.UseCases.AlertasPrecios.GetAlertasPreciosBySymbol;

public class GetAlertasPreciosBySymbolHandler(IAlertaPreciosRepository repository)
{
    public async Task<IEnumerable<GetAlertasPreciosBySymbolItem>> HandleAsync(
        string symbol,
        GetAlertasPreciosBySymbolQuery query,
        CancellationToken cancellationToken)
    {
        PagingRules.Normalize(query);

        var result = await repository.GetAlertaPreciosBySymbolAsync(query.Page, query.PageSize, symbol, cancellationToken);

        return [.. result.Items
            .Select(x => new GetAlertasPreciosBySymbolItem(
                x.CreatedAt,
                x.Symbol,
                x.PreviousTime,
                x.CurrentTime,
                x.PreviousAverage,
                x.CurrentAverage,
                x.Percentage,
                x.Direction))];
    }
}
