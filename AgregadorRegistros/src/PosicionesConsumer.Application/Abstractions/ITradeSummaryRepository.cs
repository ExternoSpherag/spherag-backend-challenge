using PosicionesConsumer.Domain.Entities;

namespace PosicionesConsumer.Application.Abstractions;

public interface ITradeSummaryRepository
{
    Task SaveAsync(TradeSummary tradeSummary, CancellationToken cancellationToken);
}
