using ApiLectura.Application.UseCases.AlertasPrecios.GetAlertasPreciosByDirection;
using ApiLectura.Application.UseCases.AlertasPrecios.GetAlertasPreciosBySymbol;
using ApiLectura.Application.UseCases.AlertasPrecios.GetAllAlertasPrecios;
using ApiLectura.Contracts.Response;

namespace ApiLectura.Mappers;
/// <summary>
/// Proporciona métodos auxiliares para convertir objetos de datos relacionados con alertas de precios en instancias de
/// respuesta de tipo AlertaPreciosResponse.
/// </summary>
/// <remarks>Esta clase estática centraliza la lógica de mapeo entre diferentes representaciones de datos de
/// alertas de precios y el modelo de respuesta utilizado por la API. Facilita la transformación consistente de datos
/// provenientes de distintas fuentes o consultas en un formato unificado para su consumo externo.</remarks>
public static class AlertaPreciosDtoMapper
{
    /// <summary>
    /// Convierte una instancia de GetAlertasPreciosBySymbolItem en un objeto AlertaPreciosResponse.
    /// </summary>
    /// <param name="result">El elemento de datos de origen que contiene la información de la alerta de precios a transformar. No puede ser
    /// null.</param>
    /// <returns>Un objeto AlertaPreciosResponse que representa la información de alerta de precios extraída de result.</returns>
    public static AlertaPreciosResponse ToResponse(GetAlertasPreciosBySymbolItem result) =>
        new()
        {
            CreatedAt = result.CreatedAt,
            Symbol = result.Symbol!,
            CurrentAverage = result.CurrentAverage,
            CurrentTime = result.CurrentTime,
            Percentage = result.Percentage,
            PreviousAverage = result.PreviousAverage,
            PreviousTime = result.PreviousTime,
            Direction = result.Direction
        };

    /// <summary>
    /// Convierte una instancia de GetAllAlertasPreciosItem en un objeto AlertaPreciosResponse.
    /// </summary>
    /// <param name="result">El objeto GetAllAlertasPreciosItem que contiene los datos de la alerta de precios a convertir.</param>
    /// <returns>Un objeto AlertaPreciosResponse que representa la alerta de precios con los datos proporcionados.</returns>
    public static AlertaPreciosResponse ToResponse(GetAllAlertasPreciosItem result) =>
        new()
        {
            CreatedAt = result.CreatedAt,
            Symbol = result.Symbol!,
            CurrentAverage = result.CurrentAverage,
            CurrentTime = result.CurrentTime,
            Percentage = result.Percentage,
            PreviousAverage = result.PreviousAverage,
            PreviousTime = result.PreviousTime,
            Direction = result.Direction
        };

    /// <summary>
    /// Convierte una instancia de GetAlertasPreciosByDirectionItem en un objeto AlertaPreciosResponse.
    /// </summary>
    /// <param name="result">El elemento de tipo GetAlertasPreciosByDirectionItem que contiene los datos de la alerta de precios a convertir.
    /// No puede ser null.</param>
    /// <returns>Un objeto AlertaPreciosResponse que representa los datos de la alerta de precios extraídos del parámetro
    /// especificado.</returns>
    public static AlertaPreciosResponse ToResponse(GetAlertasPreciosByDirectionItem result) =>
        new()
        {
            CreatedAt = result.CreatedAt,
            Symbol = result.Symbol!,
            CurrentAverage = result.CurrentAverage,
            CurrentTime = result.CurrentTime,
            Percentage = result.Percentage,
            PreviousAverage = result.PreviousAverage,
            PreviousTime = result.PreviousTime,
            Direction = result.Direction
        };
}
