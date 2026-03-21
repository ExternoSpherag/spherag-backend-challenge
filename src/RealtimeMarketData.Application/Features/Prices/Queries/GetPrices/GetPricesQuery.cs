using MediatR;
using RealtimeMarketData.Application.Common.Results;

namespace RealtimeMarketData.Application.Features.Prices.Queries.GetPrices;

public sealed record GetPricesQuery(
    string? Symbol,
    DateTimeOffset? From,
    DateTimeOffset? To) : IRequest<Result<IReadOnlyList<GetPricesResponse>>>;