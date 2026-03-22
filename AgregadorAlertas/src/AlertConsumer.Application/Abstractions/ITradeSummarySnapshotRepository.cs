using AlertConsumer.Domain.Entities;

namespace AlertConsumer.Application.Abstractions;

public interface ITradeSummarySnapshotRepository
{
    Task<TradeSummary?> GetLastBySymbolAsync(string symbol, CancellationToken cancellationToken = default);
    Task SaveAsync(TradeSummary tradeSummary, CancellationToken cancellationToken = default);
}

