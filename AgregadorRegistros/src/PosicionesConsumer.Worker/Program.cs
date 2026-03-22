using PosicionesConsumer.Application.DependencyInjection;
using PosicionesConsumer.Infrastructure.DependencyInjection;
using PosicionesConsumer.Worker.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddOptions<WorkerStartupOptions>()
    .Bind(builder.Configuration.GetSection(WorkerStartupOptions.SectionName));

builder.Services.AddPosicionesConsumerApplication();
builder.Services.AddPosicionesConsumerInfrastructure(builder.Configuration);
builder.Services.AddHostedService<TradeSummaryWorker>();

builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options =>
{
    options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
    options.SingleLine = true;
});

var host = builder.Build();
await host.RunAsync();
