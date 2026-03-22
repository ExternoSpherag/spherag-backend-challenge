using ApiLectura.Domain.Models;

namespace ApiLectura.Domain.Interfaces;

public interface IPosicionAgregadaRepository
{
    /// <summary>
    /// Asynchronously retrieves a paginated list of aggregated positions.
    /// </summary>
    /// <param name="page">The zero-based index of the page to retrieve. Must be greater than or equal to 0.</param>
    /// <param name="sizePage">The number of items to include in each page. Must be greater than 0.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a PaginatedResponse of aggregated
    /// positions for the specified page.</returns>
    Task<PaginatedResponse<PosicionAgregada>> GetAllAsync(int page, int sizePage, CancellationToken cancellationToken);
    /// <summary>
    /// Obtiene una lista paginada de posiciones agregadas que corresponden al símbolo especificado.
    /// </summary>
    /// <param name="page">El número de página que se va a recuperar. Debe ser mayor o igual que 1.</param>
    /// <param name="sizePage">La cantidad máxima de elementos por página. Debe ser mayor que 0.</param>
    /// <param name="symbol">El símbolo por el cual se filtrarán las posiciones agregadas. No puede ser nulo ni estar vacío.</param>
    /// <param name="cancellationToken">Un token que puede usarse para cancelar la operación asincrónica.</param>
    /// <returns>Una tarea que representa la operación asincrónica. El resultado contiene una respuesta paginada con las
    /// posiciones agregadas que coinciden con el símbolo especificado. Si no se encuentran coincidencias, la colección
    /// estará vacía.</returns>
    Task<PaginatedResponse<PosicionAgregada>> GetPosicionAgregadasBySymbolAsync(int page, int sizePage, string symbol, CancellationToken cancellationToken);
}