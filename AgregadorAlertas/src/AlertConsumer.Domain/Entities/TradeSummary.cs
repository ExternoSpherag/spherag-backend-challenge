namespace AlertConsumer.Domain.Entities;

public record TradeSummary
{
    public required string Symbol { get; init; }
    public required int Count { get; init; }
    public required decimal AveragePrice { get; init; }
    public required decimal TotalQuantity { get; init; }
    public required DateTimeOffset TimeUtc { get; init; }
    public required DateTimeOffset WindowStart { get; init; }
    public required DateTimeOffset WindowEnd { get; init; }
}

