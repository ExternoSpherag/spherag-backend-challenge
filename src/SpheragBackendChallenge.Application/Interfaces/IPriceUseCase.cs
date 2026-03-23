using SpheragBackendChallenge.Application.DTOs;
using SpheragBackendChallenge.Application.Results;

namespace SpheragBackendChallenge.Application.Interfaces;

public interface IPriceUseCase
{
    Task<OperationResult<IReadOnlyList<AggregatedPriceDto>>> GetPricesAsync(SymbolDateRangeDto filters, CancellationToken cancellationToken);
}
