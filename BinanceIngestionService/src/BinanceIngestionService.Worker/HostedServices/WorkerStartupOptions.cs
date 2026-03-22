namespace BinanceIngestionService.Worker.HostedServices;

public class WorkerStartupOptions
{
    public const string SectionName = "WorkerStartup";

    public int DelaySeconds { get; set; } = 20;
}
