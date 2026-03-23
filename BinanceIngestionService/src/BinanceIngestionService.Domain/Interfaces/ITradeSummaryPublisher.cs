using BinanceIngestionService.Domain.Entities;

namespace BinanceIngestionService.Domain.Interfaces;

public interface ITradeSummaryPublisher
{
    Task PublishAsync(TradeSummary summary, CancellationToken cancellationToken);
}
