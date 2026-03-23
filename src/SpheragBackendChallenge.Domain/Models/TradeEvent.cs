namespace SpheragBackendChallenge.Domain.Models;

public sealed record TradeEvent(
    string Symbol,
    decimal Price,
    decimal Quantity,
    DateTime TradeTimestampUtc,
    long? TradeId = null);
