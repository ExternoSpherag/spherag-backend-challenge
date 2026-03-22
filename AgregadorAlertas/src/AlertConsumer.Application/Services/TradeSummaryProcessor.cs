using AlertConsumer.Application.Abstractions;
using Microsoft.Extensions.Logging;
using AlertConsumer.Domain.Entities;
using AlertConsumer.Domain.Services;

namespace AlertConsumer.Application.Services;

public class TradeSummaryProcessor(
    ITradeSummarySnapshotRepository snapshotRepository,
    IPriceAlertRepository priceAlertRepository,
    PriceAlertEvaluator priceAlertEvaluator,
    ILogger<TradeSummaryProcessor> logger)
{
    public async Task ProcessAsync(TradeSummary tradeSummary, CancellationToken cancellationToken = default)
    {
        var previousTrade = await snapshotRepository.GetLastBySymbolAsync(tradeSummary.Symbol, cancellationToken);
        var evaluation = priceAlertEvaluator.Evaluate(previousTrade, tradeSummary);

        WriteTrace(evaluation, logger);

        if (evaluation.Alert is not null)
        {
            await priceAlertRepository.AddAsync(evaluation.Alert, cancellationToken);
        }

        await snapshotRepository.SaveAsync(tradeSummary, cancellationToken);
    }

    private static void WriteTrace(PriceAlertEvaluation evaluation, ILogger<TradeSummaryProcessor> logger)
    {
        var currentTrade = evaluation.CurrentTrade;

        if (evaluation.IsFirstMessage)
        {
            logger.LogInformation(
                "[{Symbol}] Primer mensaje recibido. Average actual: {AveragePrice}",
                currentTrade.Symbol,
                currentTrade.AveragePrice);
            return;
        }

        var previousTrade = evaluation.PreviousTrade!;

        logger.LogInformation(
            "[{Symbol}] Anterior: {PreviousAveragePrice} | Actual: {CurrentAveragePrice} | Diferencia: {DifferencePercentage:F2}%",
            currentTrade.Symbol,
            previousTrade.AveragePrice,
            currentTrade.AveragePrice,
            evaluation.DifferencePercentage);

        if (evaluation.ExceedsThreshold)
        {
            logger.LogWarning(
                "[{Symbol}] La diferencia supera el umbral configurado y la media es [{AveragePrice}]",
                currentTrade.Symbol,
                currentTrade.AveragePrice);
            return;
        }

        logger.LogInformation(
            "[{Symbol}] La diferencia no supera el umbral configurado y la media es [{AveragePrice}]",
            currentTrade.Symbol,
            currentTrade.AveragePrice);
    }
}

