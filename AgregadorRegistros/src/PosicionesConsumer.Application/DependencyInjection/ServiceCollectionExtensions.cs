using Microsoft.Extensions.DependencyInjection;
using PosicionesConsumer.Application.Abstractions;
using PosicionesConsumer.Application.UseCases;

namespace PosicionesConsumer.Application.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddPosicionesConsumerApplication(this IServiceCollection services)
        {
            services.AddScoped<ITradeSummaryProcessor, ProcessTradeSummaryUseCase>();
            return services;
        }
    }
}
