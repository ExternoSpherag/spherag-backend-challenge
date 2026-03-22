namespace PosicionesConsumer.Domain.Entities;

public record TradeSummary
{
    public required string Symbol { get; init; }
    public required int Count { get; init; }
    public required decimal AveragePrice { get; init; }
    public required decimal TotalQuantity { get; init; }
    public required DateTimeOffset TimeUtc { get; init; }
    public required DateTimeOffset WindowStart { get; init; }
    public required DateTimeOffset WindowEnd { get; init; }

    public void EnsureValid()
    {
        if (string.IsNullOrWhiteSpace(Symbol))
        {
            throw new InvalidOperationException("Trade summary symbol is required.");
        }

        if (Count < 0)
        {
            throw new InvalidOperationException("Trade summary count cannot be negative.");
        }

        if (AveragePrice < 0)
        {
            throw new InvalidOperationException("Trade summary average price cannot be negative.");
        }

        if (TotalQuantity < 0)
        {
            throw new InvalidOperationException("Trade summary total quantity cannot be negative.");
        }

        if (WindowEnd < WindowStart)
        {
            throw new InvalidOperationException("Trade summary window end must be greater than or equal to window start.");
        }
    }
}
