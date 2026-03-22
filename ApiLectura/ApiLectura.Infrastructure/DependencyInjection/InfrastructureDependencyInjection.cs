using ApiLectura.Domain.Interfaces;
using ApiLectura.Infrastructure.Persistence;
using ApiLectura.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ApiLectura.Infrastructure.DependencyInjection;

public static class InfrastructureDependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = Environment.GetEnvironmentVariable("POSTGRES_CONNECTION_STRING");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Environment variable 'POSTGRES_CONNECTION_STRING' not found.");
        }

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IPosicionAgregadaRepository, PosicionAgregadaRepository>();
        services.AddScoped<IAlertaPreciosRepository, AlertaPreciosRepository>();

        return services;
    }
}
