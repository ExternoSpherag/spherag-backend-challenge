using ApiLectura.Application.UseCases.PosicionesAgregadas.GetAllPosicionesAgregadas;
using ApiLectura.Application.UseCases.PosicionesAgregadas.GetPosicionesAgregadasBySymbol;
using ApiLectura.Contracts.Response;

namespace ApiLectura.Mappers;

/// <summary>
/// Provides mapping methods to convert domain model items related to aggregated positions into response DTOs for API
/// output.
/// </summary>
/// <remarks>This static class contains extension methods for transforming different types of aggregated position
/// data into a standardized response format. It is intended to centralize mapping logic and ensure consistency across
/// API responses. All methods are stateless and thread-safe.</remarks>
public static class PosicionAgregadaDtoMapper
{
    /// <summary>
    /// Converts a GetPosicionesAgregadasBySymbolItem instance to a PosicionAgregadaResponse object.
    /// </summary>
    /// <param name="result">The source GetPosicionesAgregadasBySymbolItem instance containing the aggregated position data to convert.</param>
    /// <returns>A PosicionAgregadaResponse object populated with data from the specified result parameter.</returns>
    public static PosicionAgregadaResponse ToResponse(GetPosicionesAgregadasBySymbolItem result) =>
        new()
        {
            TimeUtc = result.TimeUtc,
            Symbol = result.Symbol!,
            Count = result.Count,
            AveragePrice = result.AveragePrice,
            TotalQuantity = result.TotalQuantity,
            WindowStart = result.WindowStart,
            WindowEnd = result.WindowEnd
        };

    /// <summary>
    /// Converts a GetAllPosicionesAgregadasItem instance to a PosicionAgregadaResponse object.
    /// </summary>
    /// <param name="result">The source item containing aggregated position data to convert. Cannot be null.</param>
    /// <returns>A PosicionAgregadaResponse object populated with data from the specified result.</returns>
    public static PosicionAgregadaResponse ToResponse(GetAllPosicionesAgregadasItem result) =>
        new()
        {
            TimeUtc = result.TimeUtc,
            Symbol = result.Symbol!,
            Count = result.Count,
            AveragePrice = result.AveragePrice,
            TotalQuantity = result.TotalQuantity,
            WindowStart = result.WindowStart,
            WindowEnd = result.WindowEnd
        };

}
