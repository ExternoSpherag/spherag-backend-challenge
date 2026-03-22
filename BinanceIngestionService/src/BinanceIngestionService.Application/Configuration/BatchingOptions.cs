namespace BinanceIngestionService.Application.Configuration;

public class BatchingOptions
{
    public const string SectionName = "Batching";

    public int WindowSeconds { get; init; } = 5;
}
