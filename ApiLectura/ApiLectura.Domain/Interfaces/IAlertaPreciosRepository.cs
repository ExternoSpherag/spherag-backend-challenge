using ApiLectura.Domain.Entities;
using ApiLectura.Domain.Models;

namespace ApiLectura.Domain.Interfaces;

public interface IAlertaPreciosRepository
{
    /// <summary>
    /// Obtiene de forma asincrónica una lista paginada de alertas de precios.
    /// </summary>
    /// <param name="page">El número de página que se va a recuperar. Debe ser mayor o igual que 1.</param>
    /// <param name="sizePage">La cantidad máxima de elementos por página. Debe ser mayor que 0.</param>
    /// <param name="cancellationToken">Un token que puede usarse para cancelar la operación asincrónica.</param>
    /// <returns>Una tarea que representa la operación asincrónica. El resultado contiene una respuesta paginada con las alertas
    /// de precios correspondientes a la página solicitada.</returns>
    Task<PaginatedResponse<AlertaPrecios>> GetAllAsync(int page, int sizePage, CancellationToken cancellationToken);
    /// <summary>
    /// Obtiene una lista paginada de alertas de precios asociadas a un símbolo específico de instrumento financiero.
    /// </summary>
    /// <param name="page">El número de página que se va a recuperar. Debe ser mayor o igual que 1.</param>
    /// <param name="sizePage">La cantidad máxima de elementos por página. Debe ser mayor que 0.</param>
    /// <param name="symbol">El símbolo del instrumento financiero para el que se buscan las alertas de precios. No puede ser nulo ni estar
    /// vacío.</param>
    /// <param name="cancellationToken">Un token que puede usarse para cancelar la operación asincrónica.</param>
    /// <returns>Una tarea que representa la operación asincrónica. El resultado contiene una respuesta paginada con las alertas
    /// de precios encontradas para el símbolo especificado. Si no se encuentran alertas, la colección estará vacía.</returns>
    Task<PaginatedResponse<AlertaPrecios>> GetAlertaPreciosBySymbolAsync(int page, int sizePage, string symbol, CancellationToken cancellationToken);
    /// <summary>
    /// Obtiene una lista paginada de alertas de precios ordenadas según la dirección especificada.
    /// </summary>
    /// <param name="page">El número de página que se va a recuperar. Debe ser mayor o igual que 1.</param>
    /// <param name="sizePage">La cantidad máxima de elementos por página. Debe ser mayor que 0.</param>
    /// <param name="direction">La dirección de ordenación de los resultados. Puede ser "asc" para ascendente o "desc" para descendente.</param>
    /// <param name="cancellationToken">Un token que puede usarse para cancelar la operación asincrónica.</param>
    /// <returns>Una tarea que representa la operación asincrónica. El resultado contiene una respuesta paginada con las alertas
    /// de precios ordenadas según la dirección especificada.</returns>
    Task<PaginatedResponse<AlertaPrecios>> GetAlertaPreciosByDirectionAsync(int page, int sizePage, string direction, CancellationToken cancellationToken);
}