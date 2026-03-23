using ApiLectura.Application.UseCases.AlertasPrecios.GetAlertasPreciosByDirection;
using ApiLectura.Application.UseCases.AlertasPrecios.GetAlertasPreciosBySymbol;
using ApiLectura.Application.UseCases.AlertasPrecios.GetAllAlertasPrecios;
using ApiLectura.Application.UseCases.Prices.GetPrices;
using Microsoft.Extensions.DependencyInjection;

namespace ApiLectura.Application.DependencyInjection;

public static class ApplicationDependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<GetAllAlertasPreciosHandler>();
        services.AddScoped<GetPricesHandler>();
        services.AddScoped<GetAlertasPreciosBySymbolHandler>();
        services.AddScoped<GetAlertasPreciosByDirectionHandler>();
        return services;
    }
}
