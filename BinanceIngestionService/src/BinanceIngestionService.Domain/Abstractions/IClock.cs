namespace BinanceIngestionService.Domain.Abstractions;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
