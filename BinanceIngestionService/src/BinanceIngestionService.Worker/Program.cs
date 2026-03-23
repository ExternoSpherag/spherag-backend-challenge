using BinanceIngestionService.Application.Configuration;
using BinanceIngestionService.Application.Services;
using BinanceIngestionService.Domain.Abstractions;
using BinanceIngestionService.Domain.Interfaces;
using BinanceIngestionService.Infrastructure.Configuration;
using BinanceIngestionService.Infrastructure.MarketData;
using BinanceIngestionService.Infrastructure.Messaging;
using BinanceIngestionService.Infrastructure.Parsers;
using BinanceIngestionService.Infrastructure.Time;
using BinanceIngestionService.Worker.HostedServices;
using Microsoft.Extensions.Options;

var builder = Host.CreateApplicationBuilder(args);

var rabbitMqEnvironmentOverrides = new Dictionary<string, string?>();
AddEnvironmentOverride(rabbitMqEnvironmentOverrides, "RABBITMQ_HOST", $"{RabbitMqOptions.SectionName}:Host");
AddEnvironmentOverride(rabbitMqEnvironmentOverrides, "RABBITMQ_PORT", $"{RabbitMqOptions.SectionName}:Port");
AddEnvironmentOverride(rabbitMqEnvironmentOverrides, "RABBITMQ_USER", $"{RabbitMqOptions.SectionName}:User");
AddEnvironmentOverride(rabbitMqEnvironmentOverrides, "RABBITMQ_PASSWORD", $"{RabbitMqOptions.SectionName}:Password");

if (rabbitMqEnvironmentOverrides.Count > 0)
{
    builder.Configuration.AddInMemoryCollection(rabbitMqEnvironmentOverrides);
}

builder.Services.AddOptions<WorkerStartupOptions>()
    .Bind(builder.Configuration.GetSection(WorkerStartupOptions.SectionName));

builder.Services.AddOptions<BatchingOptions>()
    .Bind(builder.Configuration.GetSection(BatchingOptions.SectionName))
    .Validate(options => options.WindowSeconds > 0, "WindowSeconds debe ser mayor que cero.");

builder.Services.AddOptions<BinanceStreamOptions>()
    .Bind(builder.Configuration.GetSection(BinanceStreamOptions.SectionName))
    .Validate(options => !string.IsNullOrWhiteSpace(options.WebSocketUrl), "WebSocketUrl es obligatorio.");

builder.Services.AddOptions<RabbitMqOptions>()
    .Bind(builder.Configuration.GetSection(RabbitMqOptions.SectionName));

builder.Services.AddSingleton<ITradeStreamClient, BinanceTradeStreamClient>();
builder.Services.AddSingleton<ITradeMessageParser, BinanceTradeMessageParser>();
builder.Services.AddSingleton<ITradeSummaryPublisher, RabbitBatchPublisher>();
builder.Services.AddSingleton<IClock, SystemClock>();

builder.Services.AddSingleton<TradeBatchProcessor>();
builder.Services.AddSingleton<TradeStreamOrchestrator>();

builder.Services.AddHostedService<TradeWorker>();

var app = builder.Build();
await app.RunAsync();

static void AddEnvironmentOverride(
    IDictionary<string, string?> target,
    string environmentKey,
    string configurationKey)
{
    var value = Environment.GetEnvironmentVariable(environmentKey);
    if (!string.IsNullOrWhiteSpace(value))
    {
        target[configurationKey] = value;
    }
}
