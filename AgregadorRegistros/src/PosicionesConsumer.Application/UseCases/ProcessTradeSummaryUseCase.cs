using Microsoft.Extensions.Logging;
using PosicionesConsumer.Application.Abstractions;
using PosicionesConsumer.Domain.Entities;

namespace PosicionesConsumer.Application.UseCases;

public class ProcessTradeSummaryUseCase(
    ITradeSummaryRepository tradeSummaryRepository,
    ILogger<ProcessTradeSummaryUseCase> logger) : ITradeSummaryProcessor
{
    public async Task ProcessAsync(TradeSummary tradeSummary, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(tradeSummary);

        tradeSummary.EnsureValid();

        logger.LogInformation(
            "Processing trade summary for {Symbol} at {TimeUtc} with average price {AveragePrice}.",
            tradeSummary.Symbol,
            tradeSummary.TimeUtc,
            tradeSummary.AveragePrice);

        var inserted = await tradeSummaryRepository.SaveAsync(tradeSummary, cancellationToken);

        if (!inserted)
        {
            logger.LogWarning(
                "Duplicate trade summary detected for {Symbol} at {TimeUtc}. It was ignored.",
                tradeSummary.Symbol,
                tradeSummary.TimeUtc);
            return;
        }

        logger.LogInformation(
            "Trade summary for {Symbol} persisted successfully.",
            tradeSummary.Symbol);
    }
}
