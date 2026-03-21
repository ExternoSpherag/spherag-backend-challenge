using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace RealtimeMarketData.Api.Common.Errors;

internal sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is ValidationException validationException)
        {
            var details = new ProblemDetails
            {
                Title = "Validation.Failed",
                Detail = string.Join("; ", validationException.Errors.Select(e => e.ErrorMessage).Distinct()),
                Status = StatusCodes.Status400BadRequest
            };

            httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            await httpContext.Response.WriteAsJsonAsync(details, cancellationToken);
            return true;
        }

        logger.LogError(exception, "Unhandled exception");

        var problem = new ProblemDetails
        {
            Title = "Server.Error",
            Detail = "An unexpected error occurred.",
            Status = StatusCodes.Status500InternalServerError
        };

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);

        return true;
    }
}
