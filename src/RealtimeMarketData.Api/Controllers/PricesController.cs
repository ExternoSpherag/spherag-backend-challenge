using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RealtimeMarketData.Api.Common.Results;
using RealtimeMarketData.Application.Features.Prices.Queries.GetPrices;

namespace RealtimeMarketData.Api.Controllers;

[ApiController]
[Route("api/prices")]
[Authorize]
[Produces("application/json")]
public sealed class PricesController(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<GetPricesResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetPrices(
        [FromQuery] string? symbol,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        CancellationToken cancellationToken)
    {
        var query = new GetPricesQuery(symbol, from, to);
        var result = await sender.Send(query, cancellationToken);
        return result.ToActionResult(this, prices => Ok(prices));
    }
}
