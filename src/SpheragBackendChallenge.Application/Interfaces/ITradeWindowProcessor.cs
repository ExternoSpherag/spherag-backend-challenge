using SpheragBackendChallenge.Domain.Models;

namespace SpheragBackendChallenge.Application.Interfaces;

public interface ITradeWindowProcessor
{
    bool TryProcessTrade(TradeEvent tradeEvent, DateTime nowUtc);

    Task<int> FlushExpiredWindowsAsync(DateTime nowUtc, CancellationToken cancellationToken);
}
