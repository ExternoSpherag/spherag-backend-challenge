using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using PosicionesConsumer.Application.Abstractions;
using PosicionesConsumer.Application.UseCases;
using PosicionesConsumer.Infrastructure.Configuration;
using PosicionesConsumer.Infrastructure.Messaging;
using PosicionesConsumer.Infrastructure.Persistence;

namespace PosicionesConsumer.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPosicionesConsumerInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton(ResolveRabbitMqOptions(configuration));
        services.AddSingleton(ResolvePostgresOptions(configuration));
        services.AddSingleton(serviceProvider =>
        {
            var postgresOptions = serviceProvider.GetRequiredService<PostgresOptions>();
            return NpgsqlDataSource.Create(postgresOptions.ConnectionString);
        });

        services.AddSingleton<ITradeSummaryStreamConsumer, RabbitMqTradeSummaryStreamConsumer>();
        services.AddScoped<ITradeSummaryRepository, TimescaleTradeSummaryRepository>();

        return services;
    }

    private static RabbitMqOptions ResolveRabbitMqOptions(IConfiguration configuration)
    {
        var options = new RabbitMqOptions();
        configuration.GetSection(RabbitMqOptions.SectionName).Bind(options);

        options.Host = GetValue(configuration, "RabbitMq:Host", "RABBITMQ_HOST") ?? options.Host;
        options.User = GetValue(configuration, "RabbitMq:User", "RABBITMQ_USER") ?? options.User;
        options.Password = GetValue(configuration, "RabbitMq:Password", "RABBITMQ_PASSWORD") ?? options.Password;
        options.QueueName = GetValue(configuration, "RabbitMq:QueueName", "RABBITMQ_QUEUE") ?? options.QueueName;
        options.ExchangeName = GetValue(configuration, "RabbitMq:ExchangeName", "RABBITMQ_EXCHANGE") ?? options.ExchangeName;

        var portValue = GetValue(configuration, "RabbitMq:Port", "RABBITMQ_PORT");
        if (int.TryParse(portValue, out var port))
        {
            options.Port = port;
        }

        return options;
    }

    private static PostgresOptions ResolvePostgresOptions(IConfiguration configuration)
    {
        var options = new PostgresOptions();
        configuration.GetSection(PostgresOptions.SectionName).Bind(options);
        options.ConnectionString = GetValue(configuration, "Postgres:ConnectionString", "POSTGRES_CONNECTION_STRING") ?? options.ConnectionString;
        options.Schema = GetValue(configuration, "Postgres:Schema", "POSTGRES_SCHEMA") ?? options.Schema;
        options.TableName = GetValue(configuration, "Postgres:TableName", "POSTGRES_TABLE") ?? options.TableName;

        return options;
    }

    private static string? GetValue(IConfiguration configuration, string standardKey, string legacyKey)
    {
        var value = configuration[standardKey];
        return string.IsNullOrWhiteSpace(value)
            ? configuration[legacyKey]
            : value;
    }
}
