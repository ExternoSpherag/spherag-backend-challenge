using System.ComponentModel.DataAnnotations;

namespace ApiLectura.Validations;

/// <summary>
/// Specifies a set of allowed string values for a property or parameter and validates that the value matches one of the
/// specified options.
/// </summary>
/// <remarks>Use this attribute to restrict input to a predefined set of string values. Validation is
/// case-insensitive. This attribute is typically applied to properties or parameters to enforce that only specific
/// values are accepted.</remarks>
/// <param name="allowed">An array of allowed string values. The value being validated must match one of these values, case-insensitively.</param>
public class AllowedValuesAttribute(params string[] allowed) : ValidationAttribute
{
    private readonly HashSet<string> _allowed = new HashSet<string>(allowed, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Validates whether the specified value is a non-empty string and is included in the set of allowed symbols.
    /// </summary>
    /// <param name="value">The value to validate. Expected to be a non-null, non-whitespace string representing a symbol.</param>
    /// <param name="validationContext">The context information about the validation operation, including the object and member being validated.</param>
    /// <returns>A ValidationResult indicating whether the value is valid. Returns ValidationResult.Success if the value is a
    /// valid symbol; otherwise, returns a ValidationResult with an error message.</returns>
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is null)
        {
            return ValidationResult.Success;
        }

        if (value is not string symbol || string.IsNullOrWhiteSpace(symbol))
        {
            return new ValidationResult("El symbol no es válido.");
        }

        return _allowed.Contains(symbol)
            ? ValidationResult.Success
            : new ValidationResult("El symbol no es válido.");
    }
}
