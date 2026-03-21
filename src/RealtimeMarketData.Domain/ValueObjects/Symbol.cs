namespace RealtimeMarketData.Domain.ValueObjects;

public sealed record Symbol
{
    public string Value { get; }

    private Symbol(string value) => Value = value;

    public static Symbol Create(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(value));

        value = value.Trim().ToUpperInvariant();

        if (value.Length > 10)
            throw new ArgumentException("Symbol cannot exceed 10 characters.", nameof(value));

        if (!value.All(char.IsLetter))
            throw new ArgumentException("Symbol must contain only letters.", nameof(value));

        return new Symbol(value);
    }

    public override string ToString() => Value;
}
