using MediatR;
using RealtimeMarketData.Application.Common.Results;
using RealtimeMarketData.Domain.Aggregates.PriceWindow;

namespace RealtimeMarketData.Application.Features.Prices.Queries.GetPrices;

internal sealed class GetPricesQueryHandler(IPriceWindowRepository priceWindowRepository)
    : IRequestHandler<GetPricesQuery, Result<IReadOnlyList<GetPricesResponse>>>
{
    public async Task<Result<IReadOnlyList<GetPricesResponse>>> Handle(
        GetPricesQuery request,
        CancellationToken cancellationToken)
    {
        var windows = await priceWindowRepository.GetFilteredAsync(
            request.Symbol,
            request.From,
            request.To,
            cancellationToken);

        var response = windows
            .Select(w => new GetPricesResponse(w.Symbol, w.WindowStart, w.WindowEnd, w.AveragePrice))
            .ToList();

        return Result.Success<IReadOnlyList<GetPricesResponse>>(response);
    }
}