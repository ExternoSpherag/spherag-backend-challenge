namespace ApiLectura.Domain.Entities;

public class AlertaPrecios
{
    public DateTimeOffset CreatedAt { get; init; }
    public string Symbol { get; init; } = string.Empty;
    public DateTimeOffset PreviousTime { get; init; }
    public DateTimeOffset CurrentTime { get; init; }
    public decimal PreviousAverage { get; init; }
    public decimal CurrentAverage { get; init; }
    public decimal Percentage { get; init; }
    public string Direction { get; init; } = string.Empty;
}
