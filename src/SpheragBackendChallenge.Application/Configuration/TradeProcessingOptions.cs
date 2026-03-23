namespace SpheragBackendChallenge.Application.Configuration;

public sealed class TradeProcessingOptions
{
    public const string SectionName = "TradeProcessing";

    public int WindowSizeSeconds { get; set; }

    public int AllowedLatenessSeconds { get; set; }

    public int FlushIntervalSeconds { get; set; }

    public decimal AlertThresholdPercentage { get; set; }
}
