namespace ApiLectura.Application.UseCases.PosicionesAgregadas.GetPosicionesAgregadasBySymbol;

public record GetPosicionesAgregadasBySymbolItem
{
    public GetPosicionesAgregadasBySymbolItem(DateTimeOffset timeUtc, string symbol, int count, decimal averagePrice, decimal totalQuantity, 
        DateTimeOffset windowStart, DateTimeOffset windowEnd)
    {
        TimeUtc = timeUtc;
        Symbol = symbol;
        Count = count;
        AveragePrice = averagePrice;
        TotalQuantity = totalQuantity;
        WindowStart = windowStart;
        WindowEnd = windowEnd;
    }

    public DateTimeOffset TimeUtc { get; init; }
    public string? Symbol { get; init; }
    public int Count { get; init; }
    public decimal AveragePrice { get; init; }
    public decimal TotalQuantity { get; init; }
    public DateTimeOffset WindowStart { get; init; }
    public DateTimeOffset WindowEnd { get; init; }
}
