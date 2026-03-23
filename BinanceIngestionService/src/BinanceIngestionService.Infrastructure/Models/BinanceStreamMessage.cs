using System.Text.Json.Serialization;

namespace BinanceIngestionService.Infrastructure.Models;
public record BinanceStreamMessage
{
    [JsonPropertyName("stream")]
    public string? Stream { get; init; }

    [JsonPropertyName("data")]
    public BinanceTradeData? Data { get; init; }
}
