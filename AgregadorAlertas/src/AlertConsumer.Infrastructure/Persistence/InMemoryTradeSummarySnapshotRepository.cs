using AlertConsumer.Application.Abstractions;
using AlertConsumer.Domain.Entities;
using System.Collections.Concurrent;

namespace AlertConsumer.Infrastructure.Persistence;

public class InMemoryTradeSummarySnapshotRepository : ITradeSummarySnapshotRepository
{
    private readonly ConcurrentDictionary<string, TradeSummary> _snapshots = new();

    public Task<TradeSummary?> GetLastBySymbolAsync(string symbol, CancellationToken cancellationToken = default)
    {
        _snapshots.TryGetValue(symbol, out var tradeSummary);
        return Task.FromResult(tradeSummary);
    }

    public Task SaveAsync(TradeSummary tradeSummary, CancellationToken cancellationToken = default)
    {
        _snapshots[tradeSummary.Symbol] = tradeSummary;
        return Task.CompletedTask;
    }
}

