using System.Globalization;
using System.Text.Json;
using RealtimeMarketData.Application.Features.Streaming;

namespace RealtimeMarketData.Infrastructure.Streaming.Binance;

public sealed class BinanceTradeTickParser
{
    public bool TryParse(string messageJson, out TradeTick? tradeTick)
    {
        tradeTick = null;

        try
        {
            using var doc = JsonDocument.Parse(messageJson);
            var root = doc.RootElement;

            if (!root.TryGetProperty("data", out var data))
            {
                return false;
            }

            if (!data.TryGetProperty("e", out var eventTypeElement))
            {
                return false;
            }

            var eventType = eventTypeElement.GetString();
            if (!string.Equals(eventType, "trade", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!data.TryGetProperty("s", out var symbolElement) ||
                !data.TryGetProperty("p", out var priceElement) ||
                !data.TryGetProperty("q", out var quantityElement) ||
                !data.TryGetProperty("T", out var timestampElement) ||
                !data.TryGetProperty("t", out var tradeIdElement))
            {
                return false;
            }

            var symbol = symbolElement.GetString();
            var priceText = ReadAsString(priceElement);
            var quantityText = ReadAsString(quantityElement);

            if (string.IsNullOrWhiteSpace(symbol) || string.IsNullOrWhiteSpace(priceText) || string.IsNullOrWhiteSpace(quantityText))
            {
                return false;
            }

            if (!timestampElement.TryGetInt64(out var timestampMs) ||
                !tradeIdElement.TryGetInt64(out var tradeId))
            {
                return false;
            }

            if (!decimal.TryParse(priceText, NumberStyles.Number, CultureInfo.InvariantCulture, out var price) ||
                !decimal.TryParse(quantityText, NumberStyles.Number, CultureInfo.InvariantCulture, out var quantity))
            {
                return false;
            }

            if (price < 0 || quantity < 0 || tradeId <= 0)
            {
                return false;
            }

            tradeTick = new TradeTick(
                Symbol: symbol,
                Price: price,
                Quantity: quantity,
                TradeTimestamp: DateTimeOffset.FromUnixTimeMilliseconds(timestampMs),
                TradeId: tradeId);

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string? ReadAsString(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.GetRawText(),
            _ => null
        };
    }
}
