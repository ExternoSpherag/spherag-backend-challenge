using FluentValidation;
using MediatR;
using RealtimeMarketData.Application.Common.Results;

namespace RealtimeMarketData.Application.Common.Behaviors;

public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);

        var failures = (await Task.WhenAll(
                validators.Select(v => v.ValidateAsync(context, cancellationToken))))
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count == 0)
            return await next();

        var error = Error.Validation(
            "Validation.Failed",
            string.Join("; ", failures.Select(f => f.ErrorMessage).Distinct()));

        if (typeof(TResponse) == typeof(Result))
            return (TResponse)(object)Result.Failure(error);

        if (typeof(TResponse).IsGenericType && typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>))
        {
            var valueType = typeof(TResponse).GetGenericArguments()[0];
            var failureMethod = typeof(Result)
                .GetMethods()
                .Single(m =>
                    m.Name == nameof(Result.Failure)
                    && m.IsGenericMethodDefinition
                    && m.GetParameters().Length == 1);

            return (TResponse)failureMethod.MakeGenericMethod(valueType).Invoke(null, [error])!;
        }

        throw new ValidationException(failures);
    }
}
