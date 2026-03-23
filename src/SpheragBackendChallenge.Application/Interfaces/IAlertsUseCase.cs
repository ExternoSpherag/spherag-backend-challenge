using SpheragBackendChallenge.Application.DTOs;
using SpheragBackendChallenge.Application.Results;

namespace SpheragBackendChallenge.Application.Interfaces;

public interface IAlertsUseCase
{
    Task<OperationResult<IReadOnlyList<PriceAlertDto>>> GetAlertsAsync(SymbolDateRangeDto filters, CancellationToken cancellationToken);
}
