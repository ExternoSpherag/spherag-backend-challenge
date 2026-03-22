using BinanceIngestionService.Domain.Abstractions;

namespace BinanceIngestionService.Infrastructure.Time;

public class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
