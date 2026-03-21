namespace RealtimeMarketData.Domain.Aggregates.PriceWindow;

public interface IPriceWindowRepository
{
    Task<PriceWindow?> GetBySymbolAndWindowStartAsync(
        string symbol,
        DateTimeOffset windowStart,
        CancellationToken cancellationToken = default);

    Task<PriceWindow?> GetBySymbolAndWindowEndAsync(
        string symbol,
        DateTimeOffset windowEnd,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PriceWindow>> GetFilteredAsync(
        string? symbol,
        DateTimeOffset? from,
        DateTimeOffset? to,
        CancellationToken cancellationToken = default);

    Task AddAsync(PriceWindow priceWindow, CancellationToken cancellationToken = default);

    void Update(PriceWindow priceWindow);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}