using System.ComponentModel.DataAnnotations;

namespace ApiLectura.Contracts.Request;

/// <summary>
/// Represents a request specifying a direction for an operation or process.
/// </summary>
/// <remarks>The direction must be either "UP" or "DOWN". This class is typically used to indicate the intended
/// movement or orientation in APIs that require directional input.</remarks>
public class DirectionRequest
{
    /// <summary>
    /// Gets the direction for the operation.
    /// </summary>
    /// <remarks>Valid values are "UP" and "DOWN". The value is required and must be set to one of the allowed
    /// options.</remarks>
    [Required]
    [Validations.AllowedValues("DOWN", "UP")]
    public string Direction { get; init; } = string.Empty;
}
