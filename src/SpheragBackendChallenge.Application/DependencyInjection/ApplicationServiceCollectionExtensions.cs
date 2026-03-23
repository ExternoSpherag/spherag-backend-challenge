using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SpheragBackendChallenge.Application.Configuration;
using SpheragBackendChallenge.Application.Interfaces;
using SpheragBackendChallenge.Application.Services;
using SpheragBackendChallenge.Application.UseCases;

namespace SpheragBackendChallenge.Application.DependencyInjection;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<TradeProcessingOptions>(configuration.GetSection(TradeProcessingOptions.SectionName));

        services.AddScoped<IPriceUseCase, PriceUseCase>();
        services.AddScoped<IAlertsUseCase, AlertsUseCase>();
        services.AddSingleton<ITradeWindowProcessor, TradeWindowProcessor>();

        return services;
    }
}
