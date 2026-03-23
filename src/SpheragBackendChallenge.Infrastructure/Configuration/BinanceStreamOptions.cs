namespace SpheragBackendChallenge.Infrastructure.Configuration;

public sealed class BinanceStreamOptions
{
    public const string SectionName = "BinanceStream";

    public required string StreamUrl { get; set; }

    public int ReconnectDelaySeconds { get; set; }
}
