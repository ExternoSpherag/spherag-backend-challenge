using BinanceIngestionService.Domain.Entities;
using BinanceIngestionService.Domain.Interfaces;
using BinanceIngestionService.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace BinanceIngestionService.Infrastructure.Messaging;

public class RabbitBatchPublisher(IOptions<RabbitMqOptions> options, ILogger<RabbitBatchPublisher> logger) : ITradeSummaryPublisher, IAsyncDisposable
{
    private readonly RabbitMqOptions _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    private readonly ILogger<RabbitBatchPublisher> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly SemaphoreSlim _sync = new(1, 1);

    private IConnection? _connection;
    private IChannel? _channel;

    private const string exchange = "Binance";

    public async Task PublishAsync(TradeSummary summary, CancellationToken cancellationToken)
    {
        await EnsureChannelAsync(cancellationToken);

        var json = JsonSerializer.Serialize(summary);
        var body = Encoding.UTF8.GetBytes(json);
        var properties = new BasicProperties { Persistent = true };

        await _channel!.BasicPublishAsync(
            exchange: exchange,
            routingKey: string.Empty,
            mandatory: false,
            basicProperties: properties,
            body: body,
            cancellationToken: cancellationToken);

        _logger.LogInformation(
            "Resumen publicado. Símbolo: {Symbol}. Trades: {Count}. Time: {Time}",
            summary.Symbol,
            summary.Count,
            summary.TimeUtc);
    }

    private async Task EnsureChannelAsync(CancellationToken cancellationToken)
    {
        if (_channel is { IsOpen: true })
        {
            return;
        }

        await _sync.WaitAsync(cancellationToken);
        try
        {
            if (_channel is { IsOpen: true })
            {
                return;
            }

            var factory = new ConnectionFactory
            {
                HostName = _options.Host,
                Port = _options.Port,
                UserName = _options.User,
                Password = _options.Password
            };

            _connection = await factory.CreateConnectionAsync(cancellationToken);
            _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

            await _channel.ExchangeDeclareAsync(
                exchange: exchange,
                type: ExchangeType.Fanout,
                durable: true);
                       
        }
        finally
        {
            _sync.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null)
        {
            await _channel.DisposeAsync();
        }

        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }

        _sync.Dispose();
    }
}
