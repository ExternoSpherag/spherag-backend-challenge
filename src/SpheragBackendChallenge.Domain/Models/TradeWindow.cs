namespace SpheragBackendChallenge.Domain.Models;

public readonly record struct TradeWindow(DateTime StartUtc, DateTime EndUtc)
{
    public static TradeWindow FromTradeTimestamp(DateTime tradeTimestampUtc, TimeSpan windowSize)
    {
        if (tradeTimestampUtc.Kind != DateTimeKind.Utc)
        {
            tradeTimestampUtc = DateTime.SpecifyKind(tradeTimestampUtc, DateTimeKind.Utc);
        }

        var windowTicks = windowSize.Ticks;
        var alignedTicks = tradeTimestampUtc.Ticks - (tradeTimestampUtc.Ticks % windowTicks);
        var startUtc = new DateTime(alignedTicks, DateTimeKind.Utc);

        return new TradeWindow(startUtc, startUtc.Add(windowSize));
    }
}
