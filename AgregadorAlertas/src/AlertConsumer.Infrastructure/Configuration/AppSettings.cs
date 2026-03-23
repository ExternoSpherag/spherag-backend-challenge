namespace AlertConsumer.Infrastructure.Configuration;

public record AppSettings
{
    public required RabbitMqSettings RabbitMq { get; init; }
    public required PostgresSettings Postgres { get; init; }
    public decimal AlertThresholdPercentage { get; init; } = 5m;
}

