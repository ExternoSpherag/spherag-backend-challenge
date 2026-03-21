using RealtimeMarketData.Application.Features.Streaming;

namespace RealtimeMarketData.Infrastructure.Configuration;

internal sealed class PriceAlertSettings(decimal thresholdPercent) : IPriceAlertSettings
{
    public decimal ThresholdPercent { get; } = thresholdPercent;
}
