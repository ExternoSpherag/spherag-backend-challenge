using ApiLectura.Domain.Interfaces;
using ApiLectura.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace ApiLectura.Infrastructure.Persistence.Repositories;

public class PosicionAgregadaRepository(AppDbContext dbContext) : IPosicionAgregadaRepository
{
    public async Task<PaginatedResponse<PosicionAgregada>> GetAllAsync(int page, int sizePage, CancellationToken cancellationToken)
    {
        var query = dbContext.PosicionesAgregadas
            .AsNoTracking()
            .OrderByDescending(x => x.TimeUtc)
            .ThenBy(x => x.Symbol);

        var totalItems = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((page - 1) * sizePage)
            .Take(sizePage)
            .ToListAsync(cancellationToken);

        var totalPages = (int)Math.Ceiling(totalItems / (double)sizePage);
        
        return new PaginatedResponse<PosicionAgregada>
        {
            Page = page,
            SizePage = sizePage,
            TotalItems = totalItems,
            TotalPages = totalPages,
            HasPreviousPage = page > 1,
            HasNextPage = page < totalPages,
            Items = items
        };
    }

    public async Task<PaginatedResponse<PosicionAgregada>> GetPosicionAgregadasBySymbolAsync(int page, int sizePage, string symbol, CancellationToken cancellationToken)
    {

        var query = dbContext.PosicionesAgregadas
           .AsNoTracking()
           .Where(x => x.Symbol == symbol)
           .OrderByDescending(x => x.TimeUtc)
           .ThenBy(x => x.Symbol);

        var totalItems = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((page - 1) * sizePage)
            .Take(sizePage)
            .ToListAsync(cancellationToken);

        var totalPages = (int)Math.Ceiling(totalItems / (double)sizePage);

        return new PaginatedResponse<PosicionAgregada>
        {
            Page = page,
            SizePage = sizePage,
            TotalItems = totalItems,
            TotalPages = totalPages,
            HasPreviousPage = page > 1,
            HasNextPage = page < totalPages,
            Items = items
        };
    }
}
