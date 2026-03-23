using SpheragBackendChallenge.Domain.Models;

namespace SpheragBackendChallenge.Application.Interfaces;

public interface ITradeStreamClient
{
    IAsyncEnumerable<TradeEvent> StreamTradesAsync(CancellationToken cancellationToken);
}
