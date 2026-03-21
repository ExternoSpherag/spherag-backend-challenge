using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using RealtimeMarketData.Api.Common.Authentication;
using RealtimeMarketData.Api.Common.Errors;
using RealtimeMarketData.Api.Services;
using RealtimeMarketData.Application;
using RealtimeMarketData.Infrastructure;
using RealtimeMarketData.Infrastructure.Persistence;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddAuthentication(ApiKeyAuthenticationDefaults.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(
        ApiKeyAuthenticationDefaults.SchemeName,
        _ => { });
builder.Services.AddAuthorization();

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, _, _) =>
    {
        document.Info.Title = "RealtimeMarketData API";
        document.Info.Version = "v1";
        document.Info.Description = $$"""
            Real-time market data aggregation system — .NET 10
            
            ## Overview
            - Ingests trade events from Binance Futures WebSocket
            - Aggregates prices into 5-second aligned windows
            - Provides REST API to retrieve aggregated data
            - Handles duplicate trades via trade ID deduplication
            
            ## Authorization
            All endpoints require API key authentication via header:
            - Header name: `{{ApiKeyAuthenticationDefaults.HeaderName}}`
            - Format: `keyId.secret`
            - Example: `seed_default.dev-secret`
            
            ## Development Key
            For local testing: `X-Api-Key: seed_default.dev-secret`
            
            ## Data Flow
            Binance WebSocket → 5-sec aggregation → SQLite → REST API
            
            ## Edge Cases Handled
            1. **Duplicate trades**: Filtered by (symbol, tradeId, windowStart)
            2. **Disconnects**: Bounded exponential backoff (1s→2s→4s→...→30s)
            3. **Burst traffic**: Channel-based buffering (500-tick capacity)
            4. **Late events**: Rejected if outside current 5-sec window
            5. **Empty windows**: Not persisted; only windows with trades stored
            """;
        document.Components = new OpenApiComponents(document.Components)
        {
            SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme>(document.Components?.SecuritySchemes ?? new Dictionary<string, IOpenApiSecurityScheme>())
            {
                [ApiKeyAuthenticationDefaults.SchemeName] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.ApiKey,
                    In = ParameterLocation.Header,
                    Name = ApiKeyAuthenticationDefaults.HeaderName,
                    Description = "API key required in the X-Api-Key header using the format keyId.secret."
                }
            }
        };

        return Task.CompletedTask;
    });

    options.AddOperationTransformer((operation, context, _) =>
    {
        var metadata = context.Description.ActionDescriptor.EndpointMetadata;
        var allowsAnonymous = metadata.OfType<IAllowAnonymous>().Any();
        var requiresAuthorization = metadata.OfType<IAuthorizeData>().Any();

        SetProblemDetailsExample(operation, "400", "Validation.Failed", "One or more validation errors occurred.");
        SetProblemDetailsExample(operation, "401", "ApiKey.Unauthorized", "Missing or invalid API key.");
        SetProblemDetailsExample(operation, "403", "ApiKey.Forbidden", "API key is inactive or expired.");
        SetProblemDetailsExample(operation, "404", "PriceWindow.NotFound", "No aggregated price windows found for the given filter.");
        SetProblemDetailsExample(operation, "409", "PriceWindow.Conflict", "The requested operation conflicts with current resource state.");
        SetProblemDetailsExample(operation, "500", "Server.Error", "An unexpected error occurred.");

        if (allowsAnonymous || !requiresAuthorization)
        {
            return Task.CompletedTask;
        }

        operation.Security = operation.Security is null
            ? new List<OpenApiSecurityRequirement>()
            : new List<OpenApiSecurityRequirement>(operation.Security);

        operation.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference(
                ApiKeyAuthenticationDefaults.SchemeName,
                context.Document,
                null)] = new List<string>()
        });

        return Task.CompletedTask;
    });
});

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHostedService<TradeTickIngestionBackgroundService>();

var app = builder.Build();

// Apply pending EF Core migrations on startup
await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.MapScalarApiReference(options =>
    {
        options.Title = "RealtimeMarketData API";
        options.Theme = ScalarTheme.DeepSpace;
        options.DefaultHttpClient = new(ScalarTarget.CSharp, ScalarClient.HttpClient);
    });
}

app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();




static void SetProblemDetailsExample(OpenApiOperation operation, string statusCode, string title, string detail)
{
    if (operation.Responses is null || !operation.Responses.TryGetValue(statusCode, out var response))
        return;

    if (response.Content is null || response.Content.Count == 0)
        return;

    if (!response.Content.TryGetValue("application/problem+json", out var mediaType) &&
        !response.Content.TryGetValue("application/json", out mediaType))
    {
        mediaType = response.Content.Values.FirstOrDefault();
    }

    if (mediaType is null)
        return;

    mediaType.Example = new JsonObject
    {
        ["type"] = "about:blank",
        ["title"] = title,
        ["status"] = int.Parse(statusCode),
        ["detail"] = detail,
        ["instance"] = "/api/prices"
    };
}



