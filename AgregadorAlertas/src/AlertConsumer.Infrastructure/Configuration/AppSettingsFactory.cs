namespace AlertConsumer.Infrastructure.Configuration;

public static class AppSettingsFactory
{
    public static AppSettings CreateFromEnvironment()
    {
        var rabbitMqPort = int.TryParse(Environment.GetEnvironmentVariable("RABBITMQ_PORT"), out var parsedPort)
            ? parsedPort
            : 5672;

        var threshold = decimal.TryParse(Environment.GetEnvironmentVariable("ALERT_THRESHOLD_PERCENTAGE"), out var parsedThreshold)
            ? parsedThreshold
            : 5m;

        return new AppSettings
        {
            RabbitMq = new RabbitMqSettings
            {
                Host = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "localhost",
                Port = rabbitMqPort,
                User = Environment.GetEnvironmentVariable("RABBITMQ_USER") ?? "guest",
                Password = Environment.GetEnvironmentVariable("RABBITMQ_PASSWORD") ?? "guest",
                QueueName = Environment.GetEnvironmentVariable("RABBITMQ_QUEUE") ?? "alert-persistance"
            },
            Postgres = new PostgresSettings
            {
                ConnectionString = Environment.GetEnvironmentVariable("POSTGRES_CONNECTION_STRING")
                    ?? "Host=localhost;Port=5432;Database=demo_db;Username=demo_user;Password=demo_pass"
            },
            AlertThresholdPercentage = threshold
        };
    }
}

