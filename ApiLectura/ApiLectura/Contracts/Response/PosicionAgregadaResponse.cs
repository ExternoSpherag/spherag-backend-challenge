namespace ApiLectura.Contracts.Response;

/// <summary>
/// Represents the aggregated position data for a specific symbol within a defined time window.
/// </summary>
/// <remarks>This record is typically used to convey summary information about trading or inventory positions,
/// including the average price, total quantity, and the time range over which the aggregation was performed. All
/// properties are immutable and set during initialization.</remarks>
public record PosicionAgregadaResponse
{
    /// <summary>
    /// Gets the timestamp of the event in Coordinated Universal Time (UTC).
    /// </summary>
    public DateTimeOffset TimeUtc { get; init; }
    /// <summary>
    /// Gets the symbol associated with this instance.
    /// </summary>
    public string Symbol { get; init; } = string.Empty;
    /// <summary>
    /// Gets the total number of elements contained in the collection.
    /// </summary>
    public int Count { get; init; }
    /// <summary>
    /// Gets the average price calculated from the relevant data set.
    /// </summary>
    public decimal AveragePrice { get; init; }
    /// <summary>
    /// Gets the total quantity represented by this instance.
    /// </summary>
    public decimal TotalQuantity { get; init; }
    /// <summary>
    /// Gets the start time of the window interval represented by this instance.
    /// </summary>
    public DateTimeOffset WindowStart { get; init; }
    /// <summary>
    /// Gets the exclusive end timestamp of the window interval represented by this instance.
    /// </summary>
    public DateTimeOffset WindowEnd { get; init; }
}
