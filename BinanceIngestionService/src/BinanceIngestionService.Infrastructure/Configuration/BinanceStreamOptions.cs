namespace BinanceIngestionService.Infrastructure.Configuration;

public class BinanceStreamOptions
{
    public const string SectionName = "BinanceStream";

    public string WebSocketUrl { get; init; } =
        "wss://fstream.binance.com/stream?streams=btcusdt@trade/ethusdt@trade/dogeusdt@trade";

    public int ReceiveBufferSize { get; init; } = 8192;
    public int ReconnectDelaySeconds { get; init; } = 5;
}
