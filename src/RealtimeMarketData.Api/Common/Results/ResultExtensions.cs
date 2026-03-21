using Microsoft.AspNetCore.Mvc;
using RealtimeMarketData.Application.Common.Results;

namespace RealtimeMarketData.Api.Common.Results;

internal static class ResultExtensions
{
    public static IActionResult ToActionResult<T>(
        this Result<T> result,
        ControllerBase controller,
        Func<T, IActionResult> onSuccess)
    {
        if (result.IsSuccess)
            return onSuccess(result.Value!);

        return controller.Problem(
            title: result.Error.Code,
            detail: result.Error.Description,
            statusCode: MapStatusCode(result.Error.Type));
    }

    public static IActionResult ToActionResult(this Result result, ControllerBase controller)
    {
        if (result.IsSuccess)
            return controller.NoContent();

        return controller.Problem(
            title: result.Error.Code,
            detail: result.Error.Description,
            statusCode: MapStatusCode(result.Error.Type));
    }

    private static int MapStatusCode(ErrorType errorType) => errorType switch
    {
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        _ => StatusCodes.Status500InternalServerError
    };
}
