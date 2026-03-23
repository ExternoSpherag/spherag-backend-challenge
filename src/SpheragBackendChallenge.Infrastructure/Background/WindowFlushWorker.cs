using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SpheragBackendChallenge.Application.Interfaces;
using SpheragBackendChallenge.Application.Configuration;

namespace SpheragBackendChallenge.Infrastructure.Background;

public sealed class WindowFlushWorker(
    ITradeWindowProcessor tradeWindowProcessor,
    IOptions<TradeProcessingOptions> options,
    ILogger<WindowFlushWorker> logger) : BackgroundService
{
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(options.Value.FlushIntervalSeconds);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Window flush worker started with interval {Interval}", _interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var flushedCount = await tradeWindowProcessor.FlushExpiredWindowsAsync(DateTime.UtcNow, stoppingToken);
                if (flushedCount > 0)
                {
                    logger.LogInformation("Flushed {WindowCount} expired windows", flushedCount);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error while flushing windows");
            }

            try
            {
                await Task.Delay(_interval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }
}
