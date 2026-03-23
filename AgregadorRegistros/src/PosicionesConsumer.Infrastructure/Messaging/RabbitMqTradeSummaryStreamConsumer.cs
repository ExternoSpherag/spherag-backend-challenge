using System.Text.Json;
using Microsoft.Extensions.Logging;
using PosicionesConsumer.Application.Abstractions;
using PosicionesConsumer.Domain.Entities;
using PosicionesConsumer.Infrastructure.Configuration;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace PosicionesConsumer.Infrastructure.Messaging;

public class RabbitMqTradeSummaryStreamConsumer(
    RabbitMqOptions options,
    ILogger<RabbitMqTradeSummaryStreamConsumer> logger) : ITradeSummaryStreamConsumer
{
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(5);

    public async Task RunAsync(Func<TradeSummary, CancellationToken, Task> onMessage, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(onMessage);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var factory = new ConnectionFactory
                {
                    HostName = options.Host,
                    Port = options.Port,
                    UserName = options.User,
                    Password = options.Password
                };

                await using var connection = await factory.CreateConnectionAsync(cancellationToken);
                await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

                await channel.ExchangeDeclareAsync(
                    options.ExchangeName,
                    ExchangeType.Fanout,
                    durable: true,
                    cancellationToken: cancellationToken);
                await channel.QueueDeclareAsync(
                    queue: options.QueueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    cancellationToken: cancellationToken);
                await channel.QueueBindAsync(
                    queue: options.QueueName,
                    exchange: options.ExchangeName,
                    routingKey: string.Empty,
                    cancellationToken: cancellationToken);
                await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false, cancellationToken);

                logger.LogInformation(
                    "RabbitMQ consumer connected to {Host}:{Port} and waiting on queue {QueueName}.",
                    options.Host,
                    options.Port,
                    options.QueueName);

                var completionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                using var cancellationRegistration = cancellationToken.Register(() => completionSource.TrySetResult());

                var consumer = new AsyncEventingBasicConsumer(channel);
                consumer.ReceivedAsync += async (_, eventArgs) =>
                {
                    try
                    {
                        var tradeSummary = JsonSerializer.Deserialize<TradeSummary>(eventArgs.Body.Span);

                        if (tradeSummary is null)
                        {
                            throw new InvalidOperationException("Trade summary payload could not be deserialized.");
                        }

                        await onMessage(tradeSummary, cancellationToken);

                        await channel.BasicAckAsync(eventArgs.DeliveryTag, multiple: false, cancellationToken);
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                    {
                        logger.LogInformation("Cancellation requested while processing a RabbitMQ message.");
                        await channel.BasicNackAsync(eventArgs.DeliveryTag, multiple: false, requeue: true, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error processing RabbitMQ message from queue {QueueName}.", options.QueueName);
                        await channel.BasicNackAsync(eventArgs.DeliveryTag, multiple: false, requeue: true, cancellationToken);
                    }
                };

                var consumerTag = await channel.BasicConsumeAsync(
                    queue: options.QueueName,
                    autoAck: false,
                    consumer: consumer,
                    cancellationToken: cancellationToken);

                try
                {
                    await completionSource.Task.WaitAsync(cancellationToken);
                    return;
                }
                finally
                {
                    if (channel.IsOpen)
                    {
                        await channel.BasicCancelAsync(consumerTag, false, cancellationToken);
                    }
                }
            }
            catch (BrokerUnreachableException ex) when (!cancellationToken.IsCancellationRequested)
            {
                logger.LogWarning(
                    ex,
                    "RabbitMQ is not reachable at {Host}:{Port}. Retrying in {RetryDelaySeconds} seconds.",
                    options.Host,
                    options.Port,
                    RetryDelay.TotalSeconds);

                await Task.Delay(RetryDelay, cancellationToken);
            }
        }
    }
}
