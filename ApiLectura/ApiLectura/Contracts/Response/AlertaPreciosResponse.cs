namespace ApiLectura.Contracts.Response;

/// <summary>
/// Represents the response data for a price alert, including symbol information, time intervals, average prices,
/// percentage change, and direction of movement.
/// </summary>
/// <remarks>This record is typically used to convey the result of a price monitoring operation, such as when a
/// significant change in price is detected for a given symbol. All properties are immutable and set during
/// initialization.</remarks>
public record AlertaPreciosResponse
{
    /// <summary>
    /// Gets the date and time when the entity was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }
    /// <summary>
    /// Gets the symbol associated with this instance.
    /// </summary>
    public string Symbol { get; init; } = string.Empty;
    /// <summary>
    /// Gets the timestamp of the previous operation or event.
    /// </summary>
    public DateTimeOffset PreviousTime { get; init; }
    /// <summary>
    /// Gets the current point in time represented by this instance.
    /// </summary>
    public DateTimeOffset CurrentTime { get; init; }
    /// <summary>
    /// Gets the previous average value before the most recent calculation.
    /// </summary>
    public decimal PreviousAverage { get; init; }
    /// <summary>
    /// Gets the current average value calculated by the instance.
    /// </summary>
    public decimal CurrentAverage {  get; init; }
    /// <summary>
    /// Gets the percentage value represented by this property.
    /// </summary>
    public decimal Percentage { get; init; }
    /// <summary>
    /// Gets the direction associated with the current instance.
    /// </summary>
    public string Direction { get; init; } = string.Empty;
}
