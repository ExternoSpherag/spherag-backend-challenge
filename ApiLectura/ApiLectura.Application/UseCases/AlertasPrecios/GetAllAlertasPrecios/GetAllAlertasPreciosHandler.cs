using ApiLectura.Application.Common;
using ApiLectura.Domain.Interfaces;

namespace ApiLectura.Application.UseCases.AlertasPrecios.GetAllAlertasPrecios;

public class GetAllAlertasPreciosHandler(IAlertaPreciosRepository repository)
{
    public async Task<IEnumerable<GetAllAlertasPreciosItem>> HandleAsync(
        GetAllAlertasPreciosQuery query,
        CancellationToken cancellationToken)
    {
        PagingRules.Normalize(query);

        var result = await repository.GetAllAsync(query.Page, query.PageSize, cancellationToken);

        return [.. result.Items
            .Select(x => new GetAllAlertasPreciosItem(
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
