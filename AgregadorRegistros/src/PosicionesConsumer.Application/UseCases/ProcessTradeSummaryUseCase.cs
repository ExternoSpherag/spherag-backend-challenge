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

        await tradeSummaryRepository.SaveAsync(tradeSummary, cancellationToken);

        logger.LogInformation(
            "Trade summary for {Symbol} persisted successfully.",
            tradeSummary.Symbol);
    }
}
