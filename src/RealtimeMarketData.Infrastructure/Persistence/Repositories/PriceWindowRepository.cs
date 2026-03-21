using Microsoft.EntityFrameworkCore;
using RealtimeMarketData.Domain.Aggregates.PriceWindow;

namespace RealtimeMarketData.Infrastructure.Persistence.Repositories;

internal sealed class PriceWindowRepository(AppDbContext context) : IPriceWindowRepository
{
    public async Task<PriceWindow?> GetBySymbolAndWindowStartAsync(
        string symbol,
        DateTimeOffset windowStart,
        CancellationToken cancellationToken = default)
        => await context.PriceWindows
            .FirstOrDefaultAsync(
                p => p.Symbol == symbol && p.WindowStart == windowStart.ToUniversalTime(),
                cancellationToken);

    public async Task<PriceWindow?> GetBySymbolAndWindowEndAsync(
        string symbol,
        DateTimeOffset windowEnd,
        CancellationToken cancellationToken = default)
        => await context.PriceWindows
            .FirstOrDefaultAsync(
                p => p.Symbol == symbol && p.WindowEnd == windowEnd.ToUniversalTime(),
                cancellationToken);

    public async Task<IReadOnlyList<PriceWindow>> GetFilteredAsync(
        string? symbol,
        DateTimeOffset? from,
        DateTimeOffset? to,
        CancellationToken cancellationToken = default)
    {
        var query = context.PriceWindows.AsQueryable();

        if (!string.IsNullOrWhiteSpace(symbol))
            query = query.Where(p => p.Symbol == symbol);

        if (from.HasValue)
            query = query.Where(p => p.WindowStart >= from.Value.ToUniversalTime());

        if (to.HasValue)
            query = query.Where(p => p.WindowStart <= to.Value.ToUniversalTime());

        return await query
            .OrderBy(p => p.Symbol)
            .ThenBy(p => p.WindowStart)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(PriceWindow priceWindow, CancellationToken cancellationToken = default)
        => await context.PriceWindows.AddAsync(priceWindow, cancellationToken);

    public void Update(PriceWindow priceWindow)
        => context.PriceWindows.Update(priceWindow);

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => await context.SaveChangesAsync(cancellationToken);
}