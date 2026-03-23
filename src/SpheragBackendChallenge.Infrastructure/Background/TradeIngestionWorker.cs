using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SpheragBackendChallenge.Application.Interfaces;

namespace SpheragBackendChallenge.Infrastructure.Background;

public sealed class TradeIngestionWorker(
    ITradeStreamClient tradeStreamClient,
    ITradeWindowProcessor tradeWindowProcessor,
    ILogger<TradeIngestionWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Trade ingestion worker started");

        await foreach (var tradeEvent in tradeStreamClient.StreamTradesAsync(stoppingToken))
        {
            tradeWindowProcessor.TryProcessTrade(tradeEvent, DateTime.UtcNow);
        }
    }
}
