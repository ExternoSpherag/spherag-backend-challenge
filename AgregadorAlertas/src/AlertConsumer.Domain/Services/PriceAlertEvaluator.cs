using AlertConsumer.Domain.Entities;
using AlertConsumer.Domain.Enums;

namespace AlertConsumer.Domain.Services;

public class PriceAlertEvaluator(decimal thresholdPercentage)
{
    public PriceAlertEvaluation Evaluate(TradeSummary? previousTrade, TradeSummary currentTrade)
    {
        if (previousTrade is null)
        {
            return PriceAlertEvaluation.FirstMessage(currentTrade);
        }

        var differencePercentage = CalculateDifferencePercentage(previousTrade.AveragePrice, currentTrade.AveragePrice);

        if (differencePercentage <= thresholdPercentage)
        {
            return PriceAlertEvaluation.WithoutAlert(previousTrade, currentTrade, differencePercentage);
        }

        var alert = new PriceAlert
        {
            Symbol = currentTrade.Symbol,
            PreviousTimeUtc = previousTrade.TimeUtc,
            CurrentTimeUtc = currentTrade.TimeUtc,
            PreviousAveragePrice = previousTrade.AveragePrice,
            CurrentAveragePrice = currentTrade.AveragePrice,
            PercentageChange = differencePercentage,
            Direction = currentTrade.AveragePrice < previousTrade.AveragePrice
                ? PriceDirection.Down
                : PriceDirection.Up
        };

        return PriceAlertEvaluation.WithAlert(previousTrade, currentTrade, differencePercentage, alert);
    }

    private static decimal CalculateDifferencePercentage(decimal previousAverage, decimal currentAverage)
    {
        if (previousAverage == 0)
        {
            return currentAverage == 0 ? 0 : 100;
        }

        return Math.Abs((currentAverage - previousAverage) / previousAverage) * 100;
    }
}

