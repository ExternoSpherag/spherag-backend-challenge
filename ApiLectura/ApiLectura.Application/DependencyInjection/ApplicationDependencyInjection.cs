using ApiLectura.Application.UseCases.AlertasPrecios.GetAlertasPreciosByDirection;
using ApiLectura.Application.UseCases.AlertasPrecios.GetAlertasPreciosBySymbol;
using ApiLectura.Application.UseCases.AlertasPrecios.GetAllAlertasPrecios;
using ApiLectura.Application.UseCases.PosicionesAgregadas.GetAllPosicionesAgregadas;
using ApiLectura.Application.UseCases.PosicionesAgregadas.GetPosicionesAgregadasBySymbol;
using Microsoft.Extensions.DependencyInjection;

namespace ApiLectura.Application.DependencyInjection;

public static class ApplicationDependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<GetAllAlertasPreciosHandler>();
        services.AddScoped<GetAllPosicionesAgregadasHandler>();
        services.AddScoped<GetAlertasPreciosBySymbolHandler>();
        services.AddScoped<GetAlertasPreciosByDirectionHandler>();
        services.AddScoped<GetPosicionesAgregadasBySymbolHandler>();
        return services;
    }
}
