using BinanceIngestionService.Domain.Entities;

namespace BinanceIngestionService.Domain.Interfaces;

public interface ITradeMessageParser
{
    bool TryParse(string rawMessage, out TradeRow? trade);
}
