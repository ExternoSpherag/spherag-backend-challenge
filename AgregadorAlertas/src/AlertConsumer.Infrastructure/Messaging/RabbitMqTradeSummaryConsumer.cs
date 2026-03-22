using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using AlertConsumer.Application.Services;
using AlertConsumer.Domain.Entities;
using AlertConsumer.Infrastructure.Configuration;
using System.Text.Json;

namespace AlertConsumer.Infrastructure.Messaging;

public class RabbitMqTradeSummaryConsumer(
    RabbitMqSettings settings,
    TradeSummaryProcessor tradeSummaryProcessor,
    ILogger<RabbitMqTradeSummaryConsumer> logger)
{
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(5);

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var factory = new ConnectionFactory
                {
                    HostName = settings.Host,
                    Port = settings.Port,
                    UserName = settings.User,
                    Password = settings.Password
                };

                await using var connection = await factory.CreateConnectionAsync(cancellationToken);
                await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

                await channel.ExchangeDeclareAsync(
                    settings.Exchange,
                    ExchangeType.Fanout,
                    durable: true,
                    cancellationToken: cancellationToken);
                await channel.QueueDeclareAsync(
                    queue: settings.QueueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    cancellationToken: cancellationToken);
                await channel.QueueBindAsync(
                    queue: settings.QueueName,
                    exchange: settings.Exchange,
                    routingKey: string.Empty,
                    cancellationToken: cancellationToken);

                var completionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                using var cancellationRegistration = cancellationToken.Register(() => completionSource.TrySetResult());

                var consumer = new AsyncEventingBasicConsumer(channel);

                consumer.ReceivedAsync += async (_, ea) =>
                {
                    try
                    {
                        var tradeSummary = JsonSerializer.Deserialize<TradeSummary>(ea.Body.Span)
                            ?? throw new InvalidOperationException("No se pudo deserializar el mensaje recibido.");

                        logger.LogInformation(
                            "Mensaje recibido de la cola '{QueueName}': {Symbol} : {AveragePrice} : {TimeUtc}",
                            settings.QueueName,
                            tradeSummary.Symbol,
                            tradeSummary.AveragePrice,
                            tradeSummary.TimeUtc);

                        await tradeSummaryProcessor.ProcessAsync(tradeSummary, cancellationToken);

                        await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken);
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                    {
                        await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error procesando el mensaje");
                        await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true, cancellationToken);
                    }
                };

                var consumerTag = await channel.BasicConsumeAsync(
                    queue: settings.QueueName,
                    autoAck: false,
                    consumer: consumer,
                    cancellationToken: cancellationToken);

                logger.LogInformation(
                    "Esperando mensajes en la cola '{QueueName}'.",
                    settings.QueueName);

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
                    settings.Host,
                    settings.Port,
                    RetryDelay.TotalSeconds);

                await Task.Delay(RetryDelay, cancellationToken);
            }
        }
    }
}

