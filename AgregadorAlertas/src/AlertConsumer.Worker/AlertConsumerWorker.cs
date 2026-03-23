using AlertConsumer.Application.Abstractions;
using AlertConsumer.Infrastructure.Messaging;
using Microsoft.Extensions.Options;

namespace AlertConsumer.Worker;

public class AlertConsumerWorker(
    ITradeSummarySnapshotInitializer snapshotInitializer,
    RabbitMqTradeSummaryConsumer consumer,
    IOptions<WorkerStartupOptions> startupOptions,
    ILogger<AlertConsumerWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var startupDelay = TimeSpan.FromSeconds(Math.Max(0, startupOptions.Value.DelaySeconds));

        try
        {
            logger.LogInformation(
                "Esperando {StartupDelaySeconds} segundos antes de conectar con RabbitMQ.",
                startupDelay.TotalSeconds);

            await Task.Delay(startupDelay, stoppingToken);
            await snapshotInitializer.InitializeAsync(stoppingToken);

            await consumer.RunAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("Parada solicitada para el worker consumidor de alertas.");
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "El worker consumidor de alertas termino por un error no controlado.");
            throw;
        }
    }
}
