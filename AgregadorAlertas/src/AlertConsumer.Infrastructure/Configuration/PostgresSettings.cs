namespace AlertConsumer.Infrastructure.Configuration;

public record PostgresSettings
{
    public required string ConnectionString { get; init; }
}

