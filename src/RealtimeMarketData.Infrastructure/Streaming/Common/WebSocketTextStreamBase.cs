using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Extensions.Logging;

namespace RealtimeMarketData.Infrastructure.Streaming.Common;

public abstract class WebSocketTextStreamBase(ILogger logger, Uri streamUri, WebSocketStreamSettings? settings = null) : IAsyncDisposable
{
    private readonly ILogger _logger = logger;
    private readonly Uri _streamUri = streamUri;
    private readonly WebSocketStreamSettings _settings = settings ?? WebSocketStreamSettings.Default;

    private ClientWebSocket? _webSocket;
    private CancellationTokenSource? _cancellationTokenSource;

    protected virtual async IAsyncEnumerable<string> ReadTextMessagesAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _cancellationTokenSource = cts;

        var reconnectAttempts = 0;

        while (!cancellationToken.IsCancellationRequested)
        {
            if (reconnectAttempts > 0)
            {
                if (reconnectAttempts >= _settings.MaxReconnectAttempts)
                {
                    _logger.LogError(
                        "Maximum reconnection attempts ({MaxAttempts}) reached. Stopping stream.",
                        _settings.MaxReconnectAttempts);
                    break;
                }

                var backoff = BoundedBackoffPolicy.Calculate(
                    reconnectAttempts,
                    _settings.BaseReconnectDelay,
                    _settings.MaxReconnectDelay);

                _logger.LogWarning(
                    "Reconnecting in {DelaySeconds:F1}s (attempt {Attempt}/{MaxAttempts}, stream: {Url}).",
                    backoff.TotalSeconds,
                    reconnectAttempts,
                    _settings.MaxReconnectAttempts,
                    _streamUri);

                try
                {
                    await Task.Delay(backoff, cts.Token);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("WebSocket reconnect cancelled during backoff.");
                    break;
                }

                await DisconnectAsync();
            }

            var connected = false;

            try
            {
                await ConnectAsync(cts.Token);
                reconnectAttempts = 0;
                connected = true;
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("WebSocket stream reading cancelled during connect.");
                break;
            }
            catch (WebSocketException ex)
            {
                reconnectAttempts++;
                _logger.LogWarning(
                    ex,
                    "WebSocket connect failed (attempt {Attempt}/{MaxAttempts}): {Message}",
                    reconnectAttempts,
                    _settings.MaxReconnectAttempts,
                    ex.Message);
            }

            if (!connected)
                continue;

            await foreach (var message in ReceiveMessagesAsync(cts.Token))
            {
                yield return message;
            }

            if (!cancellationToken.IsCancellationRequested)
            {
                reconnectAttempts++;
                _logger.LogWarning(
                    "WebSocket stream ended unexpectedly (attempt {Attempt}/{MaxAttempts}).",
                    reconnectAttempts,
                    _settings.MaxReconnectAttempts);
            }
        }

        await DisconnectAsync();
    }

    private async Task ConnectAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Connecting to WebSocket stream: {Url}", _streamUri);

        _webSocket = new ClientWebSocket();

        using var connectCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        connectCts.CancelAfter(_settings.ConnectTimeout);

        await _webSocket.ConnectAsync(_streamUri, connectCts.Token);

        _logger.LogInformation("WebSocket stream connected: {Url}", _streamUri);
    }

    private async IAsyncEnumerable<string> ReceiveMessagesAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var buffer = new byte[_settings.ReceiveBufferSize];

        while (_webSocket?.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
        {
            WebSocketReceiveResult result;
            using var messageStream = new MemoryStream();

            do
            {
                try
                {
                    result = await _webSocket.ReceiveAsync(
                        new ArraySegment<byte>(buffer),
                        cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error while receiving WebSocket messages.");
                    throw;
                }

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    _logger.LogInformation("WebSocket close frame received from server.");
                    yield break;
                }

                if (result.Count > 0)
                {
                    await messageStream.WriteAsync(buffer.AsMemory(0, result.Count), cancellationToken);
                }
            }
            while (!result.EndOfMessage);

            if (result.MessageType != WebSocketMessageType.Text)
                continue;

            var messageJson = Encoding.UTF8.GetString(messageStream.GetBuffer(), 0, (int)messageStream.Length);
            yield return messageJson;
        }
    }

    private async Task DisconnectAsync()
    {
        if (_webSocket == null)
            return;

        try
        {
            if (_webSocket.State == WebSocketState.Open)
            {
                _logger.LogInformation("Closing WebSocket connection gracefully.");

                using var cts = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken.None);
                cts.CancelAfter(_settings.CloseTimeout);

                await _webSocket.CloseAsync(
                    WebSocketCloseStatus.NormalClosure,
                    "Closing",
                    cts.Token);
            }

            _webSocket.Dispose();
            _webSocket = null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error while closing WebSocket.");
        }
    }

    public async ValueTask DisposeAsync()
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        await DisconnectAsync();
    }
}
