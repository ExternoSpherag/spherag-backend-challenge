using System.Text.Json.Serialization;

namespace ApiLectura.Contracts.Response;

public sealed record PriceResponse
{
    [JsonPropertyName("symbol")]
    public string Symbol { get; init; } = string.Empty;

    [JsonPropertyName("window_start")]
    public DateTimeOffset WindowStart { get; init; }

    [JsonPropertyName("window_end")]
    public DateTimeOffset WindowEnd { get; init; }

    [JsonPropertyName("average_price")]
    public decimal AveragePrice { get; init; }
}
