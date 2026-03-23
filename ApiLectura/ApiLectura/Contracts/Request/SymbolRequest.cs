using System.ComponentModel.DataAnnotations;

namespace ApiLectura.Contracts.Request;

/// <summary>
/// Represents a request to specify a trading symbol for supported cryptocurrency pairs.
/// </summary>
/// <remarks>Use this class to encapsulate the symbol information when interacting with APIs that require a valid
/// trading pair. Only the symbols "BTCUSDT", "ETHUSDT", and "DOGEUSDT" are accepted. The class enforces validation to
/// ensure that only supported symbols are provided.</remarks>
public class SymbolRequest
{
    /// <summary>
    /// Gets the trading symbol for the asset pair.
    /// </summary>
    /// <remarks>The symbol must be one of the allowed values: "BTCUSDT", "ETHUSDT", or "DOGEUSDT". This
    /// property is required and cannot be empty.</remarks>
    [Required]
    [Validations.AllowedValues("BTCUSDT", "ETHUSDT", "DOGEUSDT")]
    public string Symbol { get; init; } = string.Empty;
}
