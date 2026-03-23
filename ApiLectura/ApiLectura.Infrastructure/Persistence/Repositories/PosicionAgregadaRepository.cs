using ApiLectura.Domain.Interfaces;
using ApiLectura.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace ApiLectura.Infrastructure.Persistence.Repositories;

public class PosicionAgregadaRepository(AppDbContext dbContext) : IPosicionAgregadaRepository
{
    public async Task<PaginatedResponse<PosicionAgregada>> GetPricesAsync(
        int page,
        int pageSize,
        string? symbol,
        DateTimeOffset? from,
        DateTimeOffset? to,
        CancellationToken cancellationToken)
    {
        var query = dbContext.PosicionesAgregadas
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(symbol))
        {
            query = query.Where(x => x.Symbol == symbol);
        }

        if (from.HasValue)
        {
            query = query.Where(x => x.WindowStart >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(x => x.WindowEnd <= to.Value);
        }

        var totalItems = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(x => x.WindowStart)
            .ThenBy(x => x.Symbol)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        return new PaginatedResponse<PosicionAgregada>
        {
            Page = page,
            SizePage = pageSize,
            TotalItems = totalItems,
            TotalPages = totalPages,
            HasPreviousPage = page > 1,
            HasNextPage = page < totalPages,
            Items = items
        };
    }
}
