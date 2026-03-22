namespace ApiLectura.Application.UseCases.AlertasPrecios.GetAllAlertasPrecios;

public record GetAllAlertasPreciosItem
{
    public GetAllAlertasPreciosItem(DateTimeOffset createdAt, string symbol, DateTimeOffset previousTime, DateTimeOffset currentTime, decimal previousAverage, decimal currentAverage,
        decimal percentage, string direction)
    {
        CreatedAt = createdAt;
        Symbol = symbol;
        PreviousTime = previousTime;
        CurrentTime = currentTime;
        PreviousAverage = previousAverage;
        CurrentAverage = currentAverage;
        Percentage = percentage;
        Direction = direction;
    }

    public DateTimeOffset CreatedAt { get; init; }
    public string Symbol { get; init; } = string.Empty;
    public DateTimeOffset PreviousTime { get; init; }
    public DateTimeOffset CurrentTime { get; init; }
    public decimal PreviousAverage { get; init; }
    public decimal CurrentAverage { get; init; }
    public decimal Percentage { get; init; }
    public string Direction { get; init; } = string.Empty;
}
