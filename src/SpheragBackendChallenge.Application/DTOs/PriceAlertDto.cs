namespace SpheragBackendChallenge.Application.DTOs;

public sealed class PriceAlertDto
{
    public string Symbol { get; init; } = string.Empty;

    public decimal PreviousAveragePrice { get; init; }

    public decimal CurrentAveragePrice { get; init; }

    public decimal PercentageChange { get; init; }

    public DateTime WindowStartUtc { get; init; }

    public DateTime WindowEndUtc { get; init; }

    public DateTime CreatedAtUtc { get; init; }
}
