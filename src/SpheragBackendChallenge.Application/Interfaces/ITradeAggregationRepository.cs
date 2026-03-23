using SpheragBackendChallenge.Domain.Entities;

namespace SpheragBackendChallenge.Application.Interfaces;

public interface ITradeAggregationRepository
{
    Task AddAsync(AggregatedPrice aggregatedPrice, CancellationToken cancellationToken);

    Task<AggregatedPrice?> GetLatestBeforeWindowAsync(string symbol, DateTime windowStartUtc, CancellationToken cancellationToken);

    Task<IReadOnlyList<AggregatedPrice>> QueryAsync(string? symbol, DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken);
}
