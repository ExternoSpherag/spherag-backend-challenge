namespace AlertConsumer.Infrastructure.Configuration;

public record RabbitMqSettings
{
    public required string Host { get; init; }
    public required int Port { get; init; }
    public required string User { get; init; }
    public required string Password { get; init; }
    public required string QueueName { get; init; }
    public string Exchange { get; init; } = "Binance";
}

