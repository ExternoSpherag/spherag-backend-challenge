using System.Text.Json.Serialization;

namespace RealtimeMarketData.Infrastructure.Streaming.Binance;

internal sealed class BinanceTradeTickDto
{
    [JsonPropertyName("e")]
    public string EventType { get; set; } = string.Empty;

    [JsonPropertyName("E")]
    public long EventTimestamp { get; set; }

    [JsonPropertyName("s")]
    public string Symbol { get; set; } = string.Empty;

    [JsonPropertyName("t")]
    public long TradeId { get; set; }

    [JsonPropertyName("p")]
    public string Price { get; set; } = string.Empty;

    [JsonPropertyName("q")]
    public string Quantity { get; set; } = string.Empty;

    [JsonPropertyName("T")]
    public long TradeTimestamp { get; set; }

    [JsonPropertyName("m")]
    public bool IsMakerBuyer { get; set; }

    [JsonPropertyName("M")]
    public bool IsIgnore { get; set; }
}