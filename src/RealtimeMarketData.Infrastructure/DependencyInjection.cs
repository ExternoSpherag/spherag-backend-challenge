using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RealtimeMarketData.Application.Features.Streaming;
using RealtimeMarketData.Domain.Aggregates.PriceWindow;
using RealtimeMarketData.Infrastructure.Aggregation;
using RealtimeMarketData.Infrastructure.Authentication;
using RealtimeMarketData.Infrastructure.Configuration;
using RealtimeMarketData.Infrastructure.Persistence;
using RealtimeMarketData.Infrastructure.Persistence.Interceptors;
using RealtimeMarketData.Infrastructure.Persistence.Repositories;
using RealtimeMarketData.Infrastructure.Streaming.Binance;
using RealtimeMarketData.Infrastructure.Streaming.Common;

namespace RealtimeMarketData.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Data Source=RealtimeMarketData.db";

        services.AddSingleton<AuditableEntityInterceptor>();

        services.AddDbContext<AppDbContext>((serviceProvider, options) =>
            options.UseSqlite(connectionString)
                .AddInterceptors(serviceProvider.GetRequiredService<AuditableEntityInterceptor>()));

        services.AddScoped<IApiKeyAuthenticationService, ApiKeyAuthenticationService>();
        services.AddScoped<IPriceWindowRepository, PriceWindowRepository>(); // US-03
        services.AddSingleton<ITradeWindowAggregator, InMemoryTradeWindowAggregator>();

        var priceAlertThreshold = RequirePositiveDecimal(
            configuration,
            "MarketData:Alerts:PriceChangeThresholdPercent");

        services.AddSingleton<IPriceAlertSettings>(new PriceAlertSettings(priceAlertThreshold));

        var streamUri = RequireAbsoluteUri(
            configuration,
            "MarketData:Streaming:Binance:StreamUrl");

        var webSocketSettings = new WebSocketStreamSettings(
            ReceiveBufferSize: RequirePositiveInt(configuration, "MarketData:Streaming:WebSocket:ReceiveBufferSize"),
            BaseReconnectDelay: TimeSpan.FromSeconds(RequirePositiveInt(configuration, "MarketData:Streaming:WebSocket:BaseReconnectDelaySeconds")),
            MaxReconnectDelay: TimeSpan.FromSeconds(RequirePositiveInt(configuration, "MarketData:Streaming:WebSocket:MaxReconnectDelaySeconds")),
            MaxReconnectAttempts: RequirePositiveInt(configuration, "MarketData:Streaming:WebSocket:MaxReconnectAttempts"),
            ConnectTimeout: TimeSpan.FromSeconds(RequirePositiveInt(configuration, "MarketData:Streaming:WebSocket:ConnectTimeoutSeconds")),
            CloseTimeout: TimeSpan.FromSeconds(RequirePositiveInt(configuration, "MarketData:Streaming:WebSocket:CloseTimeoutSeconds")));

        services.AddSingleton<ITradeTickStream>(serviceProvider =>
            new BinanceTradeTickStream(
                serviceProvider.GetRequiredService<ILogger<BinanceTradeTickStream>>(),
                streamUri,
                webSocketSettings));

        return services;
    }

    private static int RequirePositiveInt(IConfiguration configuration, string key)
    {
        var rawValue = configuration[key];

        if (int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) && parsed > 0)
            return parsed;

        throw new InvalidOperationException(
            $"Missing or invalid configuration '{key}'. Expected a positive integer.");
    }

    private static decimal RequirePositiveDecimal(IConfiguration configuration, string key)
    {
        var rawValue = configuration[key];

        if (decimal.TryParse(rawValue, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed) && parsed > 0)
            return parsed;

        throw new InvalidOperationException(
            $"Missing or invalid configuration '{key}'. Expected a positive decimal.");
    }

    private static Uri RequireAbsoluteUri(IConfiguration configuration, string key)
    {
        var rawValue = configuration[key];

        if (Uri.TryCreate(rawValue, UriKind.Absolute, out var uri))
            return uri;

        throw new InvalidOperationException(
            $"Missing or invalid configuration '{key}'. Expected an absolute URI.");
    }
}
