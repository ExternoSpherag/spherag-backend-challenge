using FluentAssertions;
using Xunit;
using RealtimeMarketData.Domain.ValueObjects;

namespace RealtimeMarketData.Domain.Tests.ValueObjects;

public sealed class SymbolTests
{
    [Theory]
    [InlineData("AAPL", "AAPL")]
    [InlineData("aapl", "AAPL")]
    [InlineData("Googl", "GOOGL")]
    public void Create_WithValidInput_ShouldReturnNormalizedSymbol(string input, string expected)
    {
        var symbol = Symbol.Create(input);

        symbol.Value.Should().Be(expected);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyOrWhitespace_ShouldThrowArgumentException(string input)
    {
        var act = () => Symbol.Create(input);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithNullValue_ShouldThrowArgumentException()
    {
        var act = () => Symbol.Create(null!);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithSymbolExceedingMaxLength_ShouldThrowArgumentException()
    {
        var act = () => Symbol.Create("TOOLONGVALUE");

        act.Should().Throw<ArgumentException>()
            .WithMessage("*exceed*");
    }

    [Fact]
    public void Create_WithNonLetterCharacters_ShouldThrowArgumentException()
    {
        var act = () => Symbol.Create("AP1L");

        act.Should().Throw<ArgumentException>()
            .WithMessage("*letters*");
    }

    [Fact]
    public void TwoSymbolsWithSameValue_ShouldBeEqual()
    {
        var a = Symbol.Create("AAPL");
        var b = Symbol.Create("AAPL");

        a.Should().Be(b);
    }

    [Fact]
    public void ToString_ShouldReturnSymbolValue()
    {
        var symbol = Symbol.Create("MSFT");

        symbol.ToString().Should().Be("MSFT");
    }
}
