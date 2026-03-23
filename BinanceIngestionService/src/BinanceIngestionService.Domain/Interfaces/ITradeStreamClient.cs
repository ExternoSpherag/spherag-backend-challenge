namespace BinanceIngestionService.Domain.Interfaces;
public interface ITradeStreamClient
{
    IAsyncEnumerable<string> ReadMessagesAsync(CancellationToken cancellationToken);
}
