namespace BinanceIngestionService.Application.Configuration;

public class BatchingOptions
{
    public const string SectionName = "Batching";

    public int WindowSeconds { get; init; } = 5;
    public int AllowedLatenessSeconds { get; init; } = 5;
}
