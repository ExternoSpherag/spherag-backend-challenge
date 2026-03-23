using Microsoft.EntityFrameworkCore;
using SpheragBackendChallenge.Application.Interfaces;
using SpheragBackendChallenge.Domain.Entities;
using SpheragBackendChallenge.Infrastructure.Persistence;

namespace SpheragBackendChallenge.Infrastructure.Repositories;

public sealed class TradeAggregationRepository(AppDbContext dbContext) : ITradeAggregationRepository
{
    public async Task AddAsync(AggregatedPrice aggregatedPrice, CancellationToken cancellationToken)
    {
        await dbContext.AggregatedPrices.AddAsync(aggregatedPrice, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<AggregatedPrice?> GetLatestBeforeWindowAsync(string symbol, DateTime windowStartUtc, CancellationToken cancellationToken)
    {
        return dbContext.AggregatedPrices
            .AsNoTracking()
            .Where(x => x.Symbol == symbol && x.WindowStartUtc < windowStartUtc)
            .OrderByDescending(x => x.WindowStartUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AggregatedPrice>> QueryAsync(string? symbol, DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken)
    {
        var query = dbContext.AggregatedPrices.AsNoTracking().AsQueryable();

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
            .OrderBy(x => x.WindowStartUtc)
            .ThenBy(x => x.Symbol)
            .ToListAsync(cancellationToken);
    }
}
