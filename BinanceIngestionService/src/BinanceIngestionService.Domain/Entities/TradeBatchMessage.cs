namespace BinanceIngestionService.Domain.Entities;

public record TradeBatchMessage
{
    public required DateTimeOffset BatchTimeUtc { get; init; }
    public required IReadOnlyCollection<TradeRow> Trades { get; init; }

    public int Count => Trades.Count;
}
