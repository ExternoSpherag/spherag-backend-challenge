using Microsoft.AspNetCore.Mvc;
using SpheragBackendChallenge.Application.Results;

namespace SpheragBackendChallenge.Api.Extensions;

public static class ControllerResultExtensions
{
    public static ActionResult<T> ToActionResult<T>(this ControllerBase controller, OperationResult<T> result)
    {
        if (result.IsSuccess)
        {
            return controller.Ok(result.Value);
        }

        return result.Error!.Type switch
        {
            OperationErrorType.Validation => controller.BadRequest(new { error = result.Error.Message }),
            _ => controller.Problem(detail: result.Error.Message)
        };
    }
}
