namespace SpheragBackendChallenge.Application.DTOs;

public sealed class AggregatedPriceDto
{
    public string Symbol { get; init; } = string.Empty;

    public DateTime WindowStartUtc { get; init; }

    public DateTime WindowEndUtc { get; init; }

    public decimal AveragePrice { get; init; }

    public long TradeCount { get; init; }
}
