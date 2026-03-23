using Microsoft.AspNetCore.Mvc;
using SpheragBackendChallenge.Api.Extensions;
using SpheragBackendChallenge.Application.Interfaces;
using SpheragBackendChallenge.Application.DTOs;

namespace SpheragBackendChallenge.Api.Controllers;

[ApiController]
[Route("alerts")]
public sealed class AlertsController(IAlertsUseCase alertsUseCase) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<PriceAlertDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IReadOnlyList<PriceAlertDto>>> GetAlerts(
        [FromQuery] string? symbol,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken)
    {
        var normalizedSymbol = string.IsNullOrWhiteSpace(symbol)
            ? null
            : symbol.Trim().ToUpperInvariant();

        var filters = new SymbolDateRangeDto
        {
            Symbol = normalizedSymbol,
            From = from?.ToUniversalTime(),
            To = to?.ToUniversalTime()
        };

        var result = await alertsUseCase.GetAlertsAsync(filters, cancellationToken);
        return this.ToActionResult(result);
    }
}
