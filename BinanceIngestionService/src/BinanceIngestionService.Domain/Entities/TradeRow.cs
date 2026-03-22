namespace BinanceIngestionService.Domain.Entities;

public record TradeRow
{
    public required string Stream { get; init; }
    public required string Symbol { get; init; }
    public required decimal Price { get; init; }
    public required decimal Quantity { get; init; }
    public required DateTimeOffset TradeTimeUtc { get; init; }
}
