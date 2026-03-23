using AlertConsumer.Domain.Entities;

namespace AlertConsumer.Application.Abstractions;

public interface ITradeSummarySnapshotBootstrapRepository
{
    Task<IReadOnlyList<TradeSummary>> GetLatestPerSymbolAsync(CancellationToken cancellationToken = default);
}
