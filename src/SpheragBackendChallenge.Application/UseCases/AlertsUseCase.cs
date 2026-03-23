using SpheragBackendChallenge.Application.Interfaces;
using SpheragBackendChallenge.Application.DTOs;
using SpheragBackendChallenge.Application.Results;
using SpheragBackendChallenge.Application.Validators;

namespace SpheragBackendChallenge.Application.UseCases;

public sealed class AlertsUseCase(
    IPriceAlertRepository priceAlertRepository) : IAlertsUseCase
{

    public async Task<OperationResult<IReadOnlyList<PriceAlertDto>>> GetAlertsAsync(SymbolDateRangeDto filters, CancellationToken cancellationToken)
    {
        var validationError = DateRangeValidator.Validate(filters);
        if (validationError is not null)
        {
            return OperationResult<IReadOnlyList<PriceAlertDto>>.Failure(validationError.Type, validationError.Message);
        }

        var alerts = await priceAlertRepository.QueryAsync(filters.Symbol, filters.From, filters.To, cancellationToken);

        return OperationResult<IReadOnlyList<PriceAlertDto>>.Success(alerts
            .Select(alert => new PriceAlertDto
            {
                Symbol = alert.Symbol,
                PreviousAveragePrice = alert.PreviousAveragePrice,
                CurrentAveragePrice = alert.CurrentAveragePrice,
                PercentageChange = alert.PercentageChange,
                WindowStartUtc = alert.WindowStartUtc,
                WindowEndUtc = alert.WindowEndUtc,
                CreatedAtUtc = alert.CreatedAtUtc
            })
            .ToArray());
    }
}
