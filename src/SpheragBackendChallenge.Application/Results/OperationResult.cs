namespace SpheragBackendChallenge.Application.Results;

public sealed class OperationResult<T>
{
    private OperationResult(T? value, OperationError? error)
    {
        Value = value;
        Error = error;
    }

    public bool IsSuccess => Error is null;

    public T? Value { get; }

    public OperationError? Error { get; }

    public static OperationResult<T> Success(T value) => new(value, null);

    public static OperationResult<T> Failure(OperationErrorType type, string message) => new(default, new OperationError(type, message));
}
