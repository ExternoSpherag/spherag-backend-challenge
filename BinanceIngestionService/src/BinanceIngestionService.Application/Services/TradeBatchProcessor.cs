using BinanceIngestionService.Domain.Entities;
using BinanceIngestionService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace BinanceIngestionService.Application.Services;

public class TradeBatchProcessor(
    ITradeSummaryPublisher publisher,
    ILogger<TradeBatchProcessor> logger)
{
    public async Task ProcessBatchAsync(
        IReadOnlyCollection<TradeRow> trades,
        DateTimeOffset windowStart,
        DateTimeOffset windowEnd,
        CancellationToken cancellationToken)
    {
        if (trades.Count == 0)
        {
            logger.LogDebug("Se omitio el procesamiento de una ventana vacia.");
            return;
        }

        var summaries = trades
            .GroupBy(t => t.Symbol, StringComparer.OrdinalIgnoreCase)
            .Select(group => new TradeSummary
            {
                Symbol = group.Key,
                Count = group.Count(),
                AveragePrice = group.Average(t => t.Price),
                TotalQuantity = group.Sum(t => t.Quantity),
                TimeUtc = windowEnd,
                WindowStart = windowStart,
                WindowEnd = windowEnd,
            })
            .OrderBy(summary => summary.Symbol, StringComparer.OrdinalIgnoreCase)
            .ToList();

        logger.LogInformation(
            "Procesando ventana {WindowStart:O} - {WindowEnd:O} con {TradeCount} trades validos y {SummaryCount} resumenes.",
            windowStart,
            windowEnd,
            trades.Count,
            summaries.Count);

        foreach (var summary in summaries)
        {
            await publisher.PublishAsync(summary, cancellationToken);
        }
    }
}
