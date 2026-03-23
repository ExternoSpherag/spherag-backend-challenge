using SpheragBackendChallenge.Application.Interfaces;
using SpheragBackendChallenge.Application.DTOs;
using SpheragBackendChallenge.Application.Results;
using SpheragBackendChallenge.Application.Validators;

namespace SpheragBackendChallenge.Application.UseCases;

public sealed class PriceUseCase(
    ITradeAggregationRepository tradeAggregationRepository) : IPriceUseCase
{
    public async Task<OperationResult<IReadOnlyList<AggregatedPriceDto>>> GetPricesAsync(SymbolDateRangeDto filters, CancellationToken cancellationToken)
    {
        var validationError = DateRangeValidator.Validate(filters);
        if (validationError is not null)
        {
            return OperationResult<IReadOnlyList<AggregatedPriceDto>>.Failure(validationError.Type, validationError.Message);
        }

        var prices = await tradeAggregationRepository.QueryAsync(filters.Symbol, filters.From, filters.To, cancellationToken);

        return OperationResult<IReadOnlyList<AggregatedPriceDto>>.Success(prices
            .Select(price => new AggregatedPriceDto
            {
                Symbol = price.Symbol,
                WindowStartUtc = price.WindowStartUtc,
                WindowEndUtc = price.WindowEndUtc,
                AveragePrice = price.AveragePrice,
                TradeCount = price.TradeCount
            })
            .ToArray());
    }
}
