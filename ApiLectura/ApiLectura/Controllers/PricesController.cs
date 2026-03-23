using ApiLectura.Application.UseCases.Prices.GetPrices;
using ApiLectura.Contracts.Response;
using ApiLectura.Mappers;
using Microsoft.AspNetCore.Mvc;

namespace ApiLectura.Controllers;

[ApiController]
[Route("prices")]
public class PricesController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<PriceResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<PriceResponse>>> Get(
        [FromQuery] GetPricesQuery query,
        [FromServices] GetPricesHandler handler,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await handler.HandleAsync(query, cancellationToken);
            return Ok(result.Select(PriceDtoMapper.ToResponse).ToList());
        }
        catch (ArgumentException exception)
        {
            return ValidationProblem(detail: exception.Message);
        }
    }
}
