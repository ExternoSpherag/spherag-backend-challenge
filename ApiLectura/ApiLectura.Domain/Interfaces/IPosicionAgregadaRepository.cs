using ApiLectura.Domain.Models;

namespace ApiLectura.Domain.Interfaces;

public interface IPosicionAgregadaRepository
{
    /// <summary>
     /// Obtiene posiciones agregadas filtradas opcionalmente por símbolo y rango temporal.
    /// </summary>
    Task<PaginatedResponse<PosicionAgregada>> GetPricesAsync(
        int page,
        int pageSize,
        string? symbol,
        DateTimeOffset? from,
        DateTimeOffset? to,
        CancellationToken cancellationToken);
}
