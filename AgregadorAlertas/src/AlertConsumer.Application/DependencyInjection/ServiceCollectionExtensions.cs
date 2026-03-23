using AlertConsumer.Application.Abstractions;
using AlertConsumer.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AlertConsumer.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAlertConsumerApplication(this IServiceCollection services)
    {
        services.AddSingleton<ITradeSummarySnapshotInitializer, TradeSummarySnapshotInitializer>();
        services.AddSingleton<TradeSummaryProcessor>();

        return services;
    }
}
