using ApiLectura.Domain.Entities;
using ApiLectura.Domain.Interfaces;
using ApiLectura.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace ApiLectura.Infrastructure.Persistence.Repositories;

public class AlertaPreciosRepository(AppDbContext dbContext) : IAlertaPreciosRepository
{
    public async Task<PaginatedResponse<AlertaPrecios>> GetAllAsync(int page, int sizePage, CancellationToken cancellationToken)
    {
        var query = dbContext.AlertaPrecios
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt);

        var totalItems = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((page - 1) * sizePage)
            .Take(sizePage)
            .ToListAsync(cancellationToken);

        var totalPages = (int)Math.Ceiling(totalItems / (double)sizePage);
        
        return new PaginatedResponse<AlertaPrecios>
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

    public async Task<PaginatedResponse<AlertaPrecios>> GetAlertaPreciosBySymbolAsync(int page, int sizePage, string symbol, CancellationToken cancellationToken)
    {

        var query = dbContext.AlertaPrecios
           .AsNoTracking()
           .Where(x => x.Symbol == symbol)
           .OrderByDescending(x => x.CreatedAt);

        var totalItems = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((page - 1) * sizePage)
            .Take(sizePage)
            .ToListAsync(cancellationToken);

        var totalPages = (int)Math.Ceiling(totalItems / (double)sizePage);

        return new PaginatedResponse<AlertaPrecios>
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
    public async Task<PaginatedResponse<AlertaPrecios>> GetAlertaPreciosByDirectionAsync(int page, int sizePage, string direction, CancellationToken cancellationToken)
    {

        var query = dbContext.AlertaPrecios
           .AsNoTracking()
           .Where(x => x.Direction == direction)
           .OrderByDescending(x => x.CreatedAt);

        var totalItems = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((page - 1) * sizePage)
            .Take(sizePage)
            .ToListAsync(cancellationToken);

        var totalPages = (int)Math.Ceiling(totalItems / (double)sizePage);

        return new PaginatedResponse<AlertaPrecios>
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
