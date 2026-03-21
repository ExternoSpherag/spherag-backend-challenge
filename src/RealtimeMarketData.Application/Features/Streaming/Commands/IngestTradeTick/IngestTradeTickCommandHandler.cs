using MediatR;
using Microsoft.Extensions.Logging;
using RealtimeMarketData.Application.Common.Results;
using RealtimeMarketData.Domain.Aggregates.PriceWindow;
using RealtimeMarketData.Domain.ValueObjects;

namespace RealtimeMarketData.Application.Features.Streaming.Commands.IngestTradeTick;

internal sealed class IngestTradeTickCommandHandler(
    ITradeWindowAggregator aggregator,
    IPriceWindowRepository priceWindowRepository,
    IPriceAlertSettings priceAlertSettings,
    ILogger<IngestTradeTickCommandHandler> logger)
    : IRequestHandler<IngestTradeTickCommand, Result>
{
    public async Task<Result> Handle(IngestTradeTickCommand request, CancellationToken cancellationToken)
    {
        var symbol = Symbol.Create(request.Symbol);

        var snapshot = aggregator.AddTrade(
            symbol,
            request.TradeId,
            request.Price,
            request.TradeTimestamp);

        logger.LogInformation(
            "Aggregated trade tick. AggregationId: {AggregationId}, Symbol: {Symbol}, Window: {WindowStart}-{WindowEnd}, TradeCount: {TradeCount}, AveragePrice: {AveragePrice}, Duplicate: {Duplicate}",
            snapshot.AggregationId,
            snapshot.Symbol,
            snapshot.WindowStart,
            snapshot.WindowEnd,
            snapshot.TradeCount,
            snapshot.AveragePrice,
            snapshot.IsDuplicate);

        if (!snapshot.IsDuplicate)
        {
            var previousWindow = await priceWindowRepository.GetBySymbolAndWindowEndAsync(
                snapshot.Symbol,
                snapshot.WindowStart,
                cancellationToken);

            await PersistWindowAsync(snapshot, cancellationToken);
            TryLogPriceChangeAlert(snapshot, previousWindow);
        }

        return Result.Success();
    }

    private void TryLogPriceChangeAlert(
        TradeWindowAggregationSnapshot current,
        PriceWindow? previous)
    {
        if (previous is null)
            return;

        var absoluteChangePercent = PriceChangeAlertRule.CalculateAbsoluteChangePercent(
            previous.AveragePrice,
            current.AveragePrice);

        if (!PriceChangeAlertRule.ShouldTrigger(
                previous.AveragePrice,
                current.AveragePrice,
                priceAlertSettings.ThresholdPercent))
            return;

        logger.LogWarning(
            "Price alert triggered. Symbol: {Symbol}, PreviousWindow: {PreviousWindowStart}-{PreviousWindowEnd}, CurrentWindow: {CurrentWindowStart}-{CurrentWindowEnd}, PreviousAverage: {PreviousAverage}, CurrentAverage: {CurrentAverage}, AbsoluteChangePercent: {AbsoluteChangePercent}",
            current.Symbol,
            previous.WindowStart,
            previous.WindowEnd,
            current.WindowStart,
            current.WindowEnd,
            previous.AveragePrice,
            current.AveragePrice,
            absoluteChangePercent);
    }

    private async Task PersistWindowAsync(
        TradeWindowAggregationSnapshot snapshot,
        CancellationToken cancellationToken)
    {
        var existing = await priceWindowRepository.GetBySymbolAndWindowStartAsync(
            snapshot.Symbol,
            snapshot.WindowStart,
            cancellationToken);

        if (existing is null)
        {
            var priceWindow = PriceWindow.Create(
                snapshot.AggregationId,
                snapshot.Symbol,
                snapshot.WindowStart,
                snapshot.WindowEnd,
                snapshot.AveragePrice,
                snapshot.TradeCount);

            await priceWindowRepository.AddAsync(priceWindow, cancellationToken);
        }
        else
        {
            existing.ApplySnapshot(snapshot.AveragePrice, snapshot.TradeCount);
            priceWindowRepository.Update(existing);
        }

        await priceWindowRepository.SaveChangesAsync(cancellationToken);
    }
}