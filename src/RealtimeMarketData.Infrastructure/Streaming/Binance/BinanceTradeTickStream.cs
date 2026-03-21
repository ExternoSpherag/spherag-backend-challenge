using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using RealtimeMarketData.Application.Features.Streaming;
using RealtimeMarketData.Infrastructure.Streaming.Common;

namespace RealtimeMarketData.Infrastructure.Streaming.Binance;

public class BinanceTradeTickStream(
    ILogger<BinanceTradeTickStream> logger,
    Uri? streamUri = null,
    WebSocketStreamSettings? streamSettings = null) :
    WebSocketTextStreamBase(logger, streamUri ?? new Uri(DefaultStreamUrl), streamSettings),
    ITradeTickStream
{
    private readonly ILogger<BinanceTradeTickStream> _logger = logger;
    private readonly BinanceTradeTickParser _parser = new();

    private const string DefaultStreamUrl = "wss://fstream.binance.com/stream?streams=btcusdt@trade/ethusdt@trade/dogeusdt@trade";

    public async IAsyncEnumerable<TradeTick> ReadAllAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var messageJson in ReadTextMessagesAsync(cancellationToken))
        {
            if (_parser.TryParse(messageJson, out var tradeTick) && tradeTick is not null)
            {
                yield return tradeTick;
                continue;
            }

            _logger.LogWarning("Ignoring malformed or unsupported trade message.");
        }
    }
}