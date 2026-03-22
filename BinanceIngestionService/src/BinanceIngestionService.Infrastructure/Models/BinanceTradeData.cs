using System.Text.Json.Serialization;

namespace BinanceIngestionService.Infrastructure.Models;

public record BinanceTradeData
{
    [JsonPropertyName("s")]
    public string? Symbol { get; init; }

    [JsonPropertyName("p")]
    public string? Price { get; init; }

    [JsonPropertyName("q")]
    public string? Quantity { get; init; }

    [JsonPropertyName("T")]
    public long TradeTime { get; init; }
}
