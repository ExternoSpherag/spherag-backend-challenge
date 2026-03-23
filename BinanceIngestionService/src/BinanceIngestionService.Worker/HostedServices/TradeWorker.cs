using BinanceIngestionService.Application.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BinanceIngestionService.Worker.HostedServices;

public class TradeWorker(
    TradeStreamOrchestrator orchestrator,
    IOptions<WorkerStartupOptions> startupOptions,
    ILogger<TradeWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var startupDelay = TimeSpan.FromSeconds(Math.Max(0, startupOptions.Value.DelaySeconds));

        logger.LogInformation("Worker de ingestion iniciado.");

        try
        {
            logger.LogInformation(
                "Esperando {StartupDelaySeconds} segundos antes de iniciar la ingesta.",
                startupDelay.TotalSeconds);

            await Task.Delay(startupDelay, stoppingToken);

            await orchestrator.RunAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("Worker detenido por cancelacion.");
        }
    }
}
