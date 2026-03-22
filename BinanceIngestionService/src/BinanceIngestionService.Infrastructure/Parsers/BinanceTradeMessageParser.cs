using BinanceIngestionService.Domain.Entities;
using BinanceIngestionService.Domain.Interfaces;
using BinanceIngestionService.Infrastructure.Models;
using System.Globalization;
using System.Text.Json;

namespace BinanceIngestionService.Infrastructure.Parsers;

public class BinanceTradeMessageParser : ITradeMessageParser
{
    public bool TryParse(string rawMessage, out TradeRow? trade)
    {
        trade = null;

        if (string.IsNullOrWhiteSpace(rawMessage))
        {
            return false;
        }

        try
        {
            var streamMessage = JsonSerializer.Deserialize<BinanceStreamMessage>(rawMessage);
            var data = streamMessage?.Data;

            if (data is null ||
                streamMessage is null ||
                string.IsNullOrWhiteSpace(streamMessage.Stream) ||
                string.IsNullOrWhiteSpace(data.Symbol) ||
                !decimal.TryParse(data.Price, NumberStyles.Any, CultureInfo.InvariantCulture, out var price) ||
                !decimal.TryParse(data.Quantity, NumberStyles.Any, CultureInfo.InvariantCulture, out var quantity))
            {
                return false;
            }

            trade = new TradeRow
            {
                Stream = streamMessage.Stream,
                Symbol = data.Symbol,
                Price = price,
                Quantity = quantity,
                TradeTimeUtc = DateTimeOffset.FromUnixTimeMilliseconds(data.TradeTime)
            };

            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
