using ApiLectura.Application.Common;
using ApiLectura.Domain.Interfaces;

namespace ApiLectura.Application.UseCases.AlertasPrecios.GetAlertasPreciosByDirection;

public class GetAlertasPreciosByDirectionHandler(IAlertaPreciosRepository repository)
{
    public async Task<IEnumerable<GetAlertasPreciosByDirectionItem>> HandleAsync(
        string direction,
        GetAlertasPreciosByDirectionQuery query,
        CancellationToken cancellationToken)
    {
        PagingRules.Normalize(query);

        var result = await repository.GetAlertaPreciosByDirectionAsync(query.Page, query.PageSize, direction, cancellationToken);

        return [.. result.Items
            .Select(x => new GetAlertasPreciosByDirectionItem(
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
