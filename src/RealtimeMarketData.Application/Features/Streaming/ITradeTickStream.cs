namespace RealtimeMarketData.Application.Features.Streaming;

public interface ITradeTickStream
{
    IAsyncEnumerable<TradeTick> ReadAllAsync(CancellationToken cancellationToken = default);
}