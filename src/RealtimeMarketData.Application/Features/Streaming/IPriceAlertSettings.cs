namespace RealtimeMarketData.Application.Features.Streaming;

public interface IPriceAlertSettings
{
    decimal ThresholdPercent { get; }
}
