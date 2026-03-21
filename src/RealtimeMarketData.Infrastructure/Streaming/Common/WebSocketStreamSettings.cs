namespace RealtimeMarketData.Infrastructure.Streaming.Common;

public sealed record WebSocketStreamSettings(
    int ReceiveBufferSize,
    TimeSpan BaseReconnectDelay,
    TimeSpan MaxReconnectDelay,
    int MaxReconnectAttempts,
    TimeSpan ConnectTimeout,
    TimeSpan CloseTimeout)
{
    public static readonly WebSocketStreamSettings Default = new(
        ReceiveBufferSize: 65_536,
        BaseReconnectDelay: TimeSpan.FromSeconds(1),
        MaxReconnectDelay: TimeSpan.FromSeconds(30),
        MaxReconnectAttempts: 8,
        ConnectTimeout: TimeSpan.FromSeconds(30),
        CloseTimeout: TimeSpan.FromSeconds(5));
}
