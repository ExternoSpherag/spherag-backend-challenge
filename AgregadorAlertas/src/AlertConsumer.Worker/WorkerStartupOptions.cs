namespace AlertConsumer.Worker;

public class WorkerStartupOptions
{
    public const string SectionName = "WorkerStartup";

    public int DelaySeconds { get; set; } = 20;
}
