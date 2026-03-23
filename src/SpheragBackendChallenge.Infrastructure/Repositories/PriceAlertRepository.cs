using Microsoft.EntityFrameworkCore;
using SpheragBackendChallenge.Application.Interfaces;
using SpheragBackendChallenge.Domain.Entities;
using SpheragBackendChallenge.Infrastructure.Persistence;

namespace SpheragBackendChallenge.Infrastructure.Repositories;

public sealed class PriceAlertRepository(AppDbContext dbContext) : IPriceAlertRepository
{
    public async Task AddAsync(PriceAlert alert, CancellationToken cancellationToken)
    {
        await dbContext.PriceAlerts.AddAsync(alert, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PriceAlert>> QueryAsync(string? symbol, DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken)
    {
        var query = dbContext.PriceAlerts.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(symbol))
        {
            query = query.Where(x => x.Symbol == symbol);
        }

        if (fromUtc.HasValue)
        {
            query = query.Where(x => x.WindowStartUtc >= fromUtc.Value);
        }

        if (toUtc.HasValue)
        {
            query = query.Where(x => x.WindowStartUtc <= toUtc.Value);
        }

        return await query
            .OrderByDescending(x => x.WindowStartUtc)
            .ThenBy(x => x.Symbol)
            .ToListAsync(cancellationToken);
    }
}
