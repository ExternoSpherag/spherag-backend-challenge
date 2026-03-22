using BinanceIngestionService.Domain.Interfaces;
using BinanceIngestionService.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text;

namespace BinanceIngestionService.Infrastructure.MarketData;

public class BinanceTradeStreamClient(
    IOptions<BinanceStreamOptions> options,
    ILogger<BinanceTradeStreamClient> logger) : ITradeStreamClient
{
    private readonly BinanceStreamOptions _options = options.Value;

    public async IAsyncEnumerable<string> ReadMessagesAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var buffer = new byte[_options.ReceiveBufferSize];
        var reconnectDelay = TimeSpan.FromSeconds(Math.Max(1, _options.ReconnectDelaySeconds));

        while (!cancellationToken.IsCancellationRequested)
        {
            using var webSocket = new ClientWebSocket();

            try
            {
                logger.LogInformation("Conectando al stream de Binance.");
                await webSocket.ConnectAsync(new Uri(_options.WebSocketUrl), cancellationToken);
                logger.LogInformation("Conexión con Binance establecida.");
            }
            catch (OperationCanceledException)
            {
                yield break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "No se pudo abrir la conexión con Binance.");
                await DelayReconnectAsync(reconnectDelay, cancellationToken);
                continue;
            }

            while (webSocket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
            {
                string? payload = null;

                try
                {
                    using var ms = new MemoryStream();
                    WebSocketReceiveResult result;

                    do
                    {
                        result = await webSocket.ReceiveAsync(buffer, cancellationToken);

                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            logger.LogWarning("Binance cerró la conexión WebSocket.");
                            break;
                        }

                        ms.Write(buffer, 0, result.Count);
                    }
                    while (!result.EndOfMessage);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        break;
                    }

                    payload = Encoding.UTF8.GetString(ms.ToArray());
                }
                catch (OperationCanceledException)
                {
                    yield break;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Se produjo un error mientras se leía el stream de Binance.");
                    break;
                }

                if (!string.IsNullOrWhiteSpace(payload))
                {
                    yield return payload;
                }
            }

            if (webSocket.State is WebSocketState.Open or WebSocketState.CloseReceived)
            {
                try
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Shutdown", cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    yield break;
                }
                catch (WebSocketException ex)
                {
                    logger.LogDebug(ex, "No se pudo cerrar ordenadamente el WebSocket.");
                }
            }

            await DelayReconnectAsync(reconnectDelay, cancellationToken);
        }
    }

    private async Task DelayReconnectAsync(TimeSpan reconnectDelay, CancellationToken cancellationToken)
    {
        logger.LogInformation("Reintentando conexión en {DelaySeconds} segundos.", reconnectDelay.TotalSeconds);
        await Task.Delay(reconnectDelay, cancellationToken);
    }
}
