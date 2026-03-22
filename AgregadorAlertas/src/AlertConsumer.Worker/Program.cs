using AlertConsumer.Application.Abstractions;
using AlertConsumer.Application.Services;
using AlertConsumer.Domain.Services;
using AlertConsumer.Infrastructure.Configuration;
using AlertConsumer.Infrastructure.Messaging;
using AlertConsumer.Infrastructure.Persistence;
using AlertConsumer.Worker;
using Npgsql;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddOptions<WorkerStartupOptions>()
    .Bind(builder.Configuration.GetSection(WorkerStartupOptions.SectionName));

builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options =>
{
    options.SingleLine = true;
    options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
});
builder.Logging.SetMinimumLevel(LogLevel.Information);

var settings = AppSettingsFactory.CreateFromEnvironment();

builder.Services.AddSingleton(settings);
builder.Services.AddSingleton(settings.RabbitMq);
builder.Services.AddSingleton(settings.Postgres);
builder.Services.AddSingleton(_ => NpgsqlDataSource.Create(settings.Postgres.ConnectionString));
builder.Services.AddSingleton<ITradeSummarySnapshotRepository, InMemoryTradeSummarySnapshotRepository>();
builder.Services.AddSingleton<IPriceAlertRepository, NpgsqlPriceAlertRepository>();
builder.Services.AddSingleton(new PriceAlertEvaluator(settings.AlertThresholdPercentage));
builder.Services.AddSingleton<TradeSummaryProcessor>();
builder.Services.AddSingleton<RabbitMqTradeSummaryConsumer>();
builder.Services.AddHostedService<AlertConsumerWorker>();

await builder.Build().RunAsync();

