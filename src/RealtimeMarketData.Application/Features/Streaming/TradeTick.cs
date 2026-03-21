namespace RealtimeMarketData.Application.Features.Streaming
{
    public sealed record TradeTick(
        string Symbol,
        decimal Price,
        decimal Quantity,
        DateTimeOffset TradeTimestamp,
        long TradeId);
}
