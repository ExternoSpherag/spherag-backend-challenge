using AlertConsumer.Application.Abstractions;
using AlertConsumer.Domain.Services;
using AlertConsumer.Infrastructure.Configuration;
using AlertConsumer.Infrastructure.Messaging;
using AlertConsumer.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace AlertConsumer.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAlertConsumerInfrastructure(
        this IServiceCollection services,
        AppSettings settings)
    {
        services.AddSingleton(settings);
        services.AddSingleton(settings.RabbitMq);
        services.AddSingleton(settings.Postgres);
        services.AddSingleton(_ => NpgsqlDataSource.Create(settings.Postgres.ConnectionString));
        services.AddSingleton<ITradeSummarySnapshotRepository, InMemoryTradeSummarySnapshotRepository>();
        services.AddSingleton<ITradeSummarySnapshotBootstrapRepository, NpgsqlTradeSummarySnapshotBootstrapRepository>();
        services.AddSingleton<IPriceAlertRepository, NpgsqlPriceAlertRepository>();
        services.AddSingleton(new PriceAlertEvaluator(settings.AlertThresholdPercentage));
        services.AddSingleton<RabbitMqTradeSummaryConsumer>();

        return services;
    }
}
