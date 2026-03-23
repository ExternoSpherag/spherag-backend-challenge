using PosicionesConsumer.Domain.Entities;

namespace PosicionesConsumer.Application.Abstractions;

public interface ITradeSummaryProcessor
{
    Task ProcessAsync(TradeSummary tradeSummary, CancellationToken cancellationToken);
}
