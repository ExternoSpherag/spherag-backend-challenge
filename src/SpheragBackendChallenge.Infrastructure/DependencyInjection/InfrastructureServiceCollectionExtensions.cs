using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SpheragBackendChallenge.Application.Interfaces;
using SpheragBackendChallenge.Infrastructure.Background;
using SpheragBackendChallenge.Infrastructure.Configuration;
using SpheragBackendChallenge.Infrastructure.Persistence;
using SpheragBackendChallenge.Infrastructure.Repositories;
using SpheragBackendChallenge.Infrastructure.Streaming;

namespace SpheragBackendChallenge.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<BinanceStreamOptions>(configuration.GetSection(BinanceStreamOptions.SectionName));

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<ITradeAggregationRepository, TradeAggregationRepository>();
        services.AddScoped<IPriceAlertRepository, PriceAlertRepository>();
        services.AddSingleton<ITradeStreamClient, BinanceTradeStreamClient>();

        services.AddHostedService<TradeIngestionWorker>();
        services.AddHostedService<WindowFlushWorker>();

        return services;
    }
}
