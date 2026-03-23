using ApiLectura.Application.UseCases.AlertasPrecios.GetAlertasPreciosByDirection;
using ApiLectura.Application.UseCases.AlertasPrecios.GetAlertasPreciosBySymbol;
using ApiLectura.Application.UseCases.AlertasPrecios.GetAllAlertasPrecios;
using ApiLectura.Contracts.Request;
using ApiLectura.Contracts.Response;
using ApiLectura.Mappers;
using Microsoft.AspNetCore.Mvc;

namespace ApiLectura.Controllers;

/// <summary>
/// Controlador de API que expone endpoints para consultar alertas de precios según diferentes criterios, como filtros
/// generales, símbolo o dirección.
/// </summary>
/// <remarks>Utiliza el patrón de inyección de dependencias para delegar la lógica de consulta a servicios
/// especializados. Todos los métodos devuelven respuestas HTTP estándar y admiten la cancelación de operaciones
/// asíncronas mediante tokens. Este controlador está diseñado para ser utilizado en aplicaciones que requieren la
/// gestión y consulta de alertas de precios en tiempo real o bajo demanda.</remarks>
[ApiController]
[Route("api/alertas-precios")]
public class AlertasPrecioController : ControllerBase
{
    /// <summary>
    /// Obtiene de forma asíncrona todas las alertas de precios que coinciden con los criterios especificados.
    /// </summary>
    /// <param name="query">Los parámetros de consulta que definen los criterios de filtrado y paginación para recuperar las alertas de
    /// precios.</param>
    /// <param name="handler">El servicio encargado de procesar la consulta y recuperar las alertas de precios.</param>
    /// <param name="cancellationToken">El token que puede usarse para cancelar la operación asincrónica.</param>
    /// <returns>Una acción HTTP que contiene una colección de objetos de respuesta de alerta de precios. Devuelve un código de
    /// estado 200 (OK) con la lista de alertas encontradas.</returns>
    [HttpGet("GetAll")]
    [ProducesResponseType(typeof(IEnumerable<AlertaPreciosResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<AlertaPreciosResponse>>> GetAllAsync(
        [FromQuery] GetAllAlertasPreciosQuery query,
        [FromServices] GetAllAlertasPreciosHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(query, cancellationToken);
        var response = result.Select(AlertaPreciosDtoMapper.ToResponse).ToList();
        return Ok(response);
    }

    /// <summary>
    /// Obtiene una colección de alertas de precios asociadas a un símbolo específico.
    /// </summary>
    /// <param name="request">El objeto que contiene el símbolo para el que se desean recuperar las alertas de precios. No puede ser nulo.</param>
    /// <param name="query">Los parámetros adicionales de consulta que pueden filtrar o modificar la búsqueda de alertas de precios.</param>
    /// <param name="handler">El servicio encargado de procesar la consulta y recuperar las alertas de precios correspondientes.</param>
    /// <param name="cancellationToken">El token que puede usarse para cancelar la operación de forma anticipada.</param>
    /// <returns>Un resultado de acción que contiene una colección de objetos de respuesta de alerta de precios. Devuelve un
    /// resultado con estado 200 OK y la lista de alertas encontradas; la colección estará vacía si no existen alertas
    /// para el símbolo especificado.</returns>
    [HttpGet("GetBySymbol")]
    [ProducesResponseType(typeof(IEnumerable<AlertaPreciosResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<AlertaPreciosResponse>>> GetBySymbolAsync(
        [FromQuery] SymbolRequest request,
        [FromQuery] GetAlertasPreciosBySymbolQuery query,
        [FromServices] GetAlertasPreciosBySymbolHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request.Symbol, query, cancellationToken);
        var response = result.Select(AlertaPreciosDtoMapper.ToResponse).ToList();
        return Ok(response);
    }

    /// <summary>
    /// Obtiene una colección de alertas de precios filtradas por dirección y criterios adicionales.
    /// </summary>
    /// <param name="request">El objeto que especifica la dirección por la que se filtrarán las alertas de precios. No puede ser nulo.</param>
    /// <param name="query">Los parámetros adicionales de consulta para filtrar las alertas de precios. No puede ser nulo.</param>
    /// <param name="handler">El manejador encargado de procesar la consulta y recuperar las alertas de precios correspondientes. No puede ser
    /// nulo.</param>
    /// <param name="cancellationToken">El token que puede usarse para cancelar la operación asincrónica.</param>
    /// <returns>Un resultado de acción que contiene una colección de objetos de respuesta de alertas de precios. La colección
    /// estará vacía si no se encuentran alertas que coincidan con los criterios.</returns>
    [HttpGet("GetByDirection")]
    [ProducesResponseType(typeof(IEnumerable<AlertaPreciosResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<AlertaPreciosResponse>>> GetByDirectionAsync(
        [FromQuery] DirectionRequest request,
        [FromQuery] GetAlertasPreciosByDirectionQuery query,
        [FromServices] GetAlertasPreciosByDirectionHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request.Direction, query, cancellationToken);
        var response = result.Select(AlertaPreciosDtoMapper.ToResponse).ToList();
        return Ok(response);
    }
}