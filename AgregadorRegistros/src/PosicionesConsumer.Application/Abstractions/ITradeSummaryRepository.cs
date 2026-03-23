using PosicionesConsumer.Domain.Entities;

namespace PosicionesConsumer.Application.Abstractions;

public interface ITradeSummaryRepository
{
    Task<bool> SaveAsync(TradeSummary tradeSummary, CancellationToken cancellationToken);
}
