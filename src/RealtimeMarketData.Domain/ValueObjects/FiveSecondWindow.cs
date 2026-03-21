namespace RealtimeMarketData.Domain.ValueObjects;

public sealed record FiveSecondWindow
{
    private static readonly TimeSpan WindowSize = TimeSpan.FromSeconds(5);

    private FiveSecondWindow(DateTimeOffset start, DateTimeOffset end)
    {
        Start = start;
        End = end;
    }

    public DateTimeOffset Start { get; }
    public DateTimeOffset End { get; }

    public static FiveSecondWindow FromTradeTimestamp(DateTimeOffset tradeTimestamp)
    {
        var utc = tradeTimestamp.ToUniversalTime();
        var startTicks = utc.Ticks - (utc.Ticks % WindowSize.Ticks);
        var start = new DateTimeOffset(startTicks, TimeSpan.Zero);
        return new FiveSecondWindow(start, start.Add(WindowSize));
    }
}