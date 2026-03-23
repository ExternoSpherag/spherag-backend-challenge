using AlertConsumer.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace AlertConsumer.Application.Services;

public class TradeSummarySnapshotInitializer(
    ITradeSummarySnapshotRepository snapshotRepository,
    ITradeSummarySnapshotBootstrapRepository bootstrapRepository,
    ILogger<TradeSummarySnapshotInitializer> logger) : ITradeSummarySnapshotInitializer
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var latestTradeSummaries = await bootstrapRepository.GetLatestPerSymbolAsync(cancellationToken);

        if (latestTradeSummaries.Count == 0)
        {
            logger.LogInformation("No se encontraron snapshots previos en base de datos para hidratar alertas.");
            return;
        }

        foreach (var tradeSummary in latestTradeSummaries)
        {
            await snapshotRepository.SaveAsync(tradeSummary, cancellationToken);
        }

        logger.LogInformation(
            "Snapshot de alertas hidratado desde base de datos con {Count} simbolos.",
            latestTradeSummaries.Count);
    }
}
