using Microsoft.Extensions.Options;
using PosicionesConsumer.Application.Abstractions;

namespace PosicionesConsumer.Worker.Services;

public class TradeSummaryWorker(
    ITradeSummaryStreamConsumer streamConsumer,
    IServiceScopeFactory serviceScopeFactory,
    IOptions<WorkerStartupOptions> startupOptions,
    ILogger<TradeSummaryWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var startupDelay = TimeSpan.FromSeconds(Math.Max(0, startupOptions.Value.DelaySeconds));

        logger.LogInformation("PosicionesConsumer worker started.");

        try
        {
            logger.LogInformation(
                "Waiting {StartupDelaySeconds} seconds before connecting to RabbitMQ.",
                startupDelay.TotalSeconds);

            await Task.Delay(startupDelay, stoppingToken);

            await streamConsumer.RunAsync(
                async (tradeSummary, cancellationToken) =>
                {
                    await using var scope = serviceScopeFactory.CreateAsyncScope();
                    var processor = scope.ServiceProvider.GetRequiredService<ITradeSummaryProcessor>();
                    await processor.ProcessAsync(tradeSummary, cancellationToken);
                },
                stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("PosicionesConsumer worker is stopping gracefully.");
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "PosicionesConsumer worker terminated unexpectedly.");
            throw;
        }
    }
}
