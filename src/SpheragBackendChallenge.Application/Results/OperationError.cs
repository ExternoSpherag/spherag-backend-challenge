namespace SpheragBackendChallenge.Application.Results;

public sealed record OperationError(OperationErrorType Type, string Message);
