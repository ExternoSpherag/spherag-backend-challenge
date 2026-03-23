using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SpheragBackendChallenge.Application.Interfaces;
using SpheragBackendChallenge.Domain.Models;
using SpheragBackendChallenge.Infrastructure.Configuration;
using System.Globalization;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace SpheragBackendChallenge.Infrastructure.Streaming;

public sealed class BinanceTradeStreamClient(
    IOptions<BinanceStreamOptions> options,
    ILogger<BinanceTradeStreamClient> logger) : ITradeStreamClient
{
    private readonly BinanceStreamOptions _options = options.Value;

    public async IAsyncEnumerable<TradeEvent> StreamTradesAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var streamConnection = TryCreateStreamConnection(cancellationToken);
            if (streamConnection is null)
            {
                await DelayReconnectAsync(cancellationToken);
                continue;
            }

            await using (streamConnection)
            {
                while (true)
                {
                    var trade = await TryReadNextTradeAsync(streamConnection, cancellationToken);
                    if (trade is null)
                    {
                        break;
                    }

                    yield return trade;
                }
            }

            await DelayReconnectAsync(cancellationToken);
        }
    }

    private IAsyncEnumerator<TradeEvent>? TryCreateStreamConnection(CancellationToken cancellationToken)
    {
        try
        {
            return StreamConnectionAsync(cancellationToken).GetAsyncEnumerator(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Unable to establish connection. Trying to reconnect after delay.");
            return null;
        }
    }

    private async Task<TradeEvent?> TryReadNextTradeAsync(
        IAsyncEnumerator<TradeEvent> streamConnection,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!await streamConnection.MoveNextAsync())
            {
                return null;
            }

            return streamConnection.Current;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Stream disconnected. Trying to reconnect after delay.");
            return null;
        }
    }

    private Task DelayReconnectAsync(CancellationToken cancellationToken)
    {
        return Task.Delay(TimeSpan.FromSeconds(_options.ReconnectDelaySeconds), cancellationToken);
    }

    private async IAsyncEnumerable<TradeEvent> StreamConnectionAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using var socket = new ClientWebSocket();

        logger.LogInformation("Connecting to stream at {StreamUrl}", _options.StreamUrl);
        await socket.ConnectAsync(new Uri(_options.StreamUrl), cancellationToken);
        logger.LogInformation("Connected to stream.");

        while (socket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
        {
            var payload = await ReceiveMessageAsync(socket, cancellationToken);
            if (payload is null)
            {
                logger.LogInformation("Stream closed the WebSocket connection.");
                yield break;
            }

            var trade = ParseTrade(payload, logger);
            if (trade is not null)
            {
                yield return trade;
            }
        }
    }

    private static async Task<string?> ReceiveMessageAsync(
        ClientWebSocket socket,
        CancellationToken cancellationToken)
    {
        var buffer = new byte[16 * 1024];
        using var memory = new MemoryStream();

        while (true)
        {
            var result = await socket.ReceiveAsync(buffer, cancellationToken);

            if (result.MessageType == WebSocketMessageType.Close)
            {
                if (socket.State == WebSocketState.CloseReceived)
                {
                    await socket.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "Closing connection",
                        cancellationToken);
                }

                return null;
            }

            if (result.MessageType != WebSocketMessageType.Text)
            {
                if (result.EndOfMessage)
                {
                    memory.SetLength(0);
                }

                continue;
            }

            memory.Write(buffer, 0, result.Count);

            if (result.EndOfMessage)
            {
                break;
            }
        }

        return Encoding.UTF8.GetString(memory.ToArray());
    }

    private static TradeEvent? ParseTrade(string payload, ILogger logger)
    {
        try
        {
            using var document = JsonDocument.Parse(payload);

            if (!document.RootElement.TryGetProperty("data", out var data))
            {
                return null;
            }

            if (!data.TryGetProperty("s", out var symbolProperty) ||
                !data.TryGetProperty("p", out var priceProperty) ||
                !data.TryGetProperty("q", out var quantityProperty) ||
                !data.TryGetProperty("T", out var timestampProperty))
            {
                logger.LogDebug("Ignoring payload with missing fields.");
                return null;
            }

            var symbol = symbolProperty.GetString();
            var price = priceProperty.GetString();
            var quantity = quantityProperty.GetString();

            if (string.IsNullOrWhiteSpace(symbol) ||
                !decimal.TryParse(price, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsedPrice) ||
                !decimal.TryParse(quantity, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsedQuantity))
            {
                return null;
            }

            if (parsedPrice <= 0 || parsedQuantity <= 0)
            {
                return null;
            }

            var tradeTimestamp = DateTimeOffset
                .FromUnixTimeMilliseconds(timestampProperty.GetInt64())
                .UtcDateTime;

            long? tradeId = null;

            if (data.TryGetProperty("t", out var tradeIdProperty) &&
                tradeIdProperty.ValueKind == JsonValueKind.Number)
            {
                tradeId = tradeIdProperty.GetInt64();
            }

            return new TradeEvent(
                symbol.ToUpperInvariant(),
                parsedPrice,
                parsedQuantity,
                tradeTimestamp,
                tradeId);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to parse trade payload.");
            return null;
        }
    }
}
