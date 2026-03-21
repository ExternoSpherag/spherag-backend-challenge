using System.Globalization;
using System.Threading.Channels;
using MediatR;
using RealtimeMarketData.Application.Features.Streaming;
using RealtimeMarketData.Application.Features.Streaming.Commands.IngestTradeTick;

namespace RealtimeMarketData.Api.Services;

public sealed class TradeTickIngestionBackgroundService(
    ITradeTickStream stream,
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<TradeTickIngestionBackgroundService> logger) : BackgroundService
{
    private readonly int _channelCapacity = ParseChannelCapacity(configuration);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "Trade tick ingestion background service starting. ChannelCapacity: {Capacity}.",
            _channelCapacity);

        var channel = Channel.CreateBounded<TradeTick>(new BoundedChannelOptions(_channelCapacity)
        {
            FullMode     = BoundedChannelFullMode.DropWrite,
            SingleReader = true,
            SingleWriter = true
        });

        try
        {
            await Task.WhenAll(
                ProduceAsync(channel.Writer, stoppingToken),
                ConsumeAsync(channel.Reader, stoppingToken));
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Trade tick ingestion cancelled.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fatal error in trade tick ingestion background service.");
            throw;
        }
        finally
        {
            logger.LogInformation("Trade tick ingestion background service stopped.");
        }
    }

    private async Task ProduceAsync(ChannelWriter<TradeTick> writer, CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var tradeTick in stream.ReadAllAsync(cancellationToken))
            {
                if (!writer.TryWrite(tradeTick))
                {
                    logger.LogWarning(
                        "Channel full ({Capacity}). Dropping incoming tick. Symbol: {Symbol}, TradeId: {TradeId}, Price: {Price}.",
                        _channelCapacity,
                        tradeTick.Symbol,
                        tradeTick.TradeId,
                        tradeTick.Price);
                }
            }
        }
        finally
        {
            writer.Complete();
            logger.LogInformation("Trade tick producer completed.");
        }
    }

    private async Task ConsumeAsync(ChannelReader<TradeTick> reader, CancellationToken cancellationToken)
    {
        await foreach (var tradeTick in reader.ReadAllAsync(cancellationToken))
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var mediator = scope.ServiceProvider.GetRequiredService<ISender>();

                var command = new IngestTradeTickCommand(
                    Symbol:         tradeTick.Symbol,
                    Price:          tradeTick.Price,
                    Quantity:       tradeTick.Quantity,
                    TradeTimestamp: tradeTick.TradeTimestamp,
                    TradeId:        tradeTick.TradeId);

                await mediator.Send(command, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Error processing trade tick. Symbol: {Symbol}, TradeId: {TradeId}, Price: {Price}.",
                    tradeTick.Symbol,
                    tradeTick.TradeId,
                    tradeTick.Price);
            }
        }

        logger.LogInformation("Trade tick consumer completed.");
    }

    private static int ParseChannelCapacity(IConfiguration configuration)
    {
        var rawValue = configuration["MarketData:Ingestion:ChannelCapacity"];

        if (int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var capacity) && capacity > 0)
            return capacity;

        throw new InvalidOperationException(
            "Missing or invalid configuration 'MarketData:Ingestion:ChannelCapacity'. Expected a positive integer.");
    }
}