using AlertConsumer.Domain.Entities;

namespace AlertConsumer.Domain.Services;

public record PriceAlertEvaluation
{
    public required bool IsFirstMessage { get; init; }
    public required TradeSummary? PreviousTrade { get; init; }
    public required TradeSummary CurrentTrade { get; init; }
    public required decimal DifferencePercentage { get; init; }
    public required bool ExceedsThreshold { get; init; }
    public required PriceAlert? Alert { get; init; }

    public static PriceAlertEvaluation FirstMessage(TradeSummary currentTrade) =>
        new()
        {
            IsFirstMessage = true,
            PreviousTrade = null,
            CurrentTrade = currentTrade,
            DifferencePercentage = 0,
            ExceedsThreshold = false,
            Alert = null
        };

    public static PriceAlertEvaluation WithoutAlert(
        TradeSummary previousTrade,
        TradeSummary currentTrade,
        decimal differencePercentage) =>
        new()
        {
            IsFirstMessage = false,
            PreviousTrade = previousTrade,
            CurrentTrade = currentTrade,
            DifferencePercentage = differencePercentage,
            ExceedsThreshold = false,
            Alert = null
        };

    public static PriceAlertEvaluation WithAlert(
        TradeSummary previousTrade,
        TradeSummary currentTrade,
        decimal differencePercentage,
        PriceAlert alert) =>
        new()
        {
            IsFirstMessage = false,
            PreviousTrade = previousTrade,
            CurrentTrade = currentTrade,
            DifferencePercentage = differencePercentage,
            ExceedsThreshold = true,
            Alert = alert
        };
}

