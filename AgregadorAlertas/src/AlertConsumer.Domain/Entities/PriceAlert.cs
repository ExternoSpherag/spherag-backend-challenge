using AlertConsumer.Domain.Enums;

namespace AlertConsumer.Domain.Entities;

public record PriceAlert
{
    public required string Symbol { get; init; }
    public required DateTimeOffset PreviousTimeUtc { get; init; }
    public required DateTimeOffset CurrentTimeUtc { get; init; }
    public required decimal PreviousAveragePrice { get; init; }
    public required decimal CurrentAveragePrice { get; init; }
    public required decimal PercentageChange { get; init; }
    public required PriceDirection Direction { get; init; }
}

