using SpheragBackendChallenge.Domain.Entities;

namespace SpheragBackendChallenge.Application.Interfaces;

public interface IPriceAlertRepository
{
    Task AddAsync(PriceAlert alert, CancellationToken cancellationToken);

    Task<IReadOnlyList<PriceAlert>> QueryAsync(string? symbol, DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken);
}
