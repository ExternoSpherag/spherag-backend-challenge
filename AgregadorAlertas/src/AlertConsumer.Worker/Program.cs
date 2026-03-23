using AlertConsumer.Application.DependencyInjection;
using AlertConsumer.Infrastructure.Configuration;
using AlertConsumer.Infrastructure.DependencyInjection;
using AlertConsumer.Worker;

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

builder.Services.AddAlertConsumerApplication();
builder.Services.AddAlertConsumerInfrastructure(settings);
builder.Services.AddHostedService<AlertConsumerWorker>();

await builder.Build().RunAsync();
