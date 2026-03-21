namespace RealtimeMarketData.Infrastructure.Streaming.Common;

public static class BoundedBackoffPolicy
{
    public static TimeSpan Calculate(int attempt, TimeSpan baseDelay, TimeSpan maxDelay)
    {
        if (attempt <= 0)
            return baseDelay;

        var exponentialSeconds = baseDelay.TotalSeconds * Math.Pow(2, attempt - 1);
        var cappedSeconds = Math.Min(exponentialSeconds, maxDelay.TotalSeconds);

        return TimeSpan.FromSeconds(cappedSeconds);
    }
}