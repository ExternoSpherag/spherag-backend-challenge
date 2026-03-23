using SpheragBackendChallenge.Application.DTOs;
using SpheragBackendChallenge.Application.Results;

namespace SpheragBackendChallenge.Application.Validators;

public static class DateRangeValidator
{
    public static OperationError? Validate(SymbolDateRangeDto filters)
    {
        if (filters.From.HasValue && filters.To.HasValue && filters.From.Value > filters.To.Value)
        {
            return new OperationError(
                OperationErrorType.Validation,
                "The 'from' date must be earlier than or equal to the 'to' date.");
        }

        return null;
    }
}
