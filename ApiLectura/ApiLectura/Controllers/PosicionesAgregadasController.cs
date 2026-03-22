using ApiLectura.Application.UseCases.PosicionesAgregadas.GetAllPosicionesAgregadas;
using ApiLectura.Application.UseCases.PosicionesAgregadas.GetPosicionesAgregadasBySymbol;
using ApiLectura.Contracts.Request;
using ApiLectura.Contracts.Response;
using ApiLectura.Mappers;
using Microsoft.AspNetCore.Mvc;

namespace ApiLectura.Controllers;

/// <summary>
/// Controlador de API que expone endpoints para consultar posiciones agregadas. Proporciona operaciones para recuperar
/// todas las posiciones agregadas o filtrarlas por símbolo.
/// </summary>
/// <remarks>Utiliza el patrón de consulta y manejador para separar la lógica de negocio de la capa de
/// presentación. Los métodos de este controlador devuelven respuestas HTTP con los datos de posiciones agregadas en
/// formato JSON. Adecuado para integraciones de clientes que requieren información consolidada de posiciones.</remarks>
[ApiController]
[Route("api/posiciones-agregadas")]
public class PosicionesAgregadasController : ControllerBase
{
    /// <summary>
    /// Obtiene todas las posiciones agregadas que coinciden con los criterios de consulta especificados.
    /// </summary>
    /// <param name="query">Los parámetros de consulta que determinan los filtros y opciones de búsqueda para las posiciones agregadas.</param>
    /// <param name="handler">El servicio encargado de procesar la consulta y recuperar las posiciones agregadas.</param>
    /// <param name="cancellationToken">El token que puede usarse para cancelar la operación asincrónica.</param>
    /// <returns>Un resultado de acción que contiene una colección de respuestas de posiciones agregadas. Devuelve un código de
    /// estado 200 (OK) con la lista de posiciones si la operación es exitosa.</returns>
    [HttpGet("GetAll")]
    [ProducesResponseType(typeof(IEnumerable<PosicionAgregadaResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<PosicionAgregadaResponse>>> GetAll(
        [FromQuery] GetAllPosicionesAgregadasQuery query,
        [FromServices] GetAllPosicionesAgregadasHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(query, cancellationToken);
        var response = result.Select(PosicionAgregadaDtoMapper.ToResponse).ToList();
        return Ok(response);
    }

    /// <summary>
    /// Obtiene una colección de posiciones agregadas filtradas por símbolo.
    /// </summary>
    /// <param name="request">El objeto que contiene el símbolo por el cual se filtrarán las posiciones agregadas. No puede ser nulo.</param>
    /// <param name="query">Los parámetros adicionales de consulta que determinan los criterios de filtrado y paginación para la búsqueda de
    /// posiciones agregadas.</param>
    /// <param name="handler">El servicio encargado de procesar la consulta y recuperar las posiciones agregadas correspondientes.</param>
    /// <param name="cancellationToken">El token que puede usarse para cancelar la operación asincrónica.</param>
    /// <returns>Una respuesta HTTP que contiene una colección de objetos de tipo PosicionAgregadaResponse correspondientes al
    /// símbolo especificado. Devuelve un resultado vacío si no se encuentran posiciones.</returns>
    [HttpGet("GetBySymbol")]
    [ProducesResponseType(typeof(IEnumerable<PosicionAgregadaResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<PosicionAgregadaResponse>>> GetBySymbol(
        [FromQuery] SymbolRequest request,
        [FromQuery] GetPosicionesAgregadasBySymbolQuery query,
        [FromServices] GetPosicionesAgregadasBySymbolHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request.Symbol, query, cancellationToken);
        var response = result.Select(PosicionAgregadaDtoMapper.ToResponse).ToList();
        return Ok(response);
    }
}