using System.Text.Json.Serialization;

namespace RealtimeMarketData.Application.Features.Prices.Queries.GetPrices;

public sealed record GetPricesResponse(
    [property: JsonPropertyName("symbol")] string Symbol,
    [property: JsonPropertyName("window_start")] DateTimeOffset WindowStart,
    [property: JsonPropertyName("window_end")] DateTimeOffset WindowEnd,
    [property: JsonPropertyName("average_price")] decimal AveragePrice);