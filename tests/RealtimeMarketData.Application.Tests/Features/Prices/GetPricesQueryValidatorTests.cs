using FluentAssertions;
using RealtimeMarketData.Application.Features.Prices.Queries.GetPrices;
using Xunit;

namespace RealtimeMarketData.Application.Tests.Features.Prices;

public sealed class GetPricesQueryValidatorTests
{
    private readonly GetPricesQueryValidator _validator = new();

    [Fact]
    public async Task Validate_NoFilters_IsValid()
    {
        var result = await _validator.ValidateAsync(new GetPricesQuery(null, null, null));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_FromEqualsTo_IsValid()
    {
        var dt = DateTimeOffset.Parse("2026-01-01T12:00:00Z");
        var result = await _validator.ValidateAsync(new GetPricesQuery(null, dt, dt));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_FromBeforeTo_IsValid()
    {
        var result = await _validator.ValidateAsync(new GetPricesQuery(
            null,
            DateTimeOffset.Parse("2026-01-01T12:00:00Z"),
            DateTimeOffset.Parse("2026-01-01T12:05:00Z")));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_FromAfterTo_IsInvalid_WithExpectedMessage()
    {
        var result = await _validator.ValidateAsync(new GetPricesQuery(
            null,
            DateTimeOffset.Parse("2026-01-01T12:05:00Z"),
            DateTimeOffset.Parse("2026-01-01T12:00:00Z")));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e =>
            e.ErrorMessage == "The 'from' value must be lower than or equal to 'to'.");
    }

    [Fact]
    public async Task Validate_OnlyFrom_IsValid()
    {
        var result = await _validator.ValidateAsync(
            new GetPricesQuery(null, DateTimeOffset.Parse("2026-01-01T12:00:00Z"), null));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_OnlyTo_IsValid()
    {
        var result = await _validator.ValidateAsync(
            new GetPricesQuery(null, null, DateTimeOffset.Parse("2026-01-01T12:05:00Z")));
        result.IsValid.Should().BeTrue();
    }
}