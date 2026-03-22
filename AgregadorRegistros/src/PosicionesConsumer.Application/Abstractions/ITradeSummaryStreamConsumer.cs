using PosicionesConsumer.Domain.Entities;

namespace PosicionesConsumer.Application.Abstractions;

public interface ITradeSummaryStreamConsumer
{
    Task RunAsync(Func<TradeSummary, CancellationToken, Task> onMessage, CancellationToken cancellationToken);
}
