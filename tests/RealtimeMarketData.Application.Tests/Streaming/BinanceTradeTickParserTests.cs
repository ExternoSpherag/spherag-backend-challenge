using FluentAssertions;
using RealtimeMarketData.Infrastructure.Streaming.Binance;
using Xunit;

namespace RealtimeMarketData.Application.Tests.Streaming;

public sealed class BinanceTradeTickParserTests
{
    private readonly BinanceTradeTickParser _parser = new();

    [Fact]
    public void TryParse_WithValidBtcTradeMessage_ShouldReturnAllFieldsCorrect()
    {
        var result = _parser.TryParse(ValidBtcTradeMessage(), out var tradeTick);

        result.Should().BeTrue();
        tradeTick.Should().NotBeNull();
        tradeTick!.Symbol.Should().Be("BTCUSDT");
        tradeTick.Price.Should().Be(67321.11m);
        tradeTick.Quantity.Should().Be(0.250m);
        tradeTick.TradeId.Should().Be(12345L);
        tradeTick.TradeTimestamp.ToUnixTimeMilliseconds().Should().Be(1672515782136L);
    }

    [Theory]
    [InlineData("ETHUSDT", "2450.50", "1.500", 54321L, 1672515783000L)]
    [InlineData("DOGEUSDT", "0.15", "10000.00", 99999L, 1672515784000L)]
    public void TryParse_WithValidAltcoinTradeMessage_ShouldReturnCorrectTick(
        string symbol, string price, string quantity, long tradeId, long timestampMs)
    {
        var json = BuildTradeMessage(symbol, price, quantity, tradeId, timestampMs);

        var result = _parser.TryParse(json, out var tradeTick);

        result.Should().BeTrue();
        tradeTick!.Symbol.Should().Be(symbol);
        tradeTick.TradeId.Should().Be(tradeId);
        tradeTick.TradeTimestamp.ToUnixTimeMilliseconds().Should().Be(timestampMs);
    }

    [Fact]
    public void TryParse_WithMissingDataField_ShouldReturnFalse()
    {
        var result = _parser.TryParse("""{"stream":"btcusdt@trade"}""", out var tradeTick);

        result.Should().BeFalse();
        tradeTick.Should().BeNull();
    }

    [Fact]
    public void TryParse_WithMissingEventTypeField_ShouldReturnFalse()
    {
        var json = """
            {
              "stream": "btcusdt@trade",
              "data": {
                "E": 1672515782136,
                "s": "BTCUSDT",
                "t": 12345,
                "p": "67321.11",
                "q": "0.250",
                "T": 1672515782136
              }
            }
            """;

        var result = _parser.TryParse(json, out var tradeTick);

        result.Should().BeFalse();
        tradeTick.Should().BeNull();
    }

    [Fact]
    public void TryParse_WithNonTradeEventType_ShouldReturnFalse()
    {
        var json = """
            {
              "stream": "btcusdt@kline",
              "data": {
                "e": "kline",
                "E": 1672515782136,
                "s": "BTCUSDT",
                "t": 12345,
                "p": "67321.11",
                "q": "0.250",
                "T": 1672515782136
              }
            }
            """;

        var result = _parser.TryParse(json, out var tradeTick);

        result.Should().BeFalse();
        tradeTick.Should().BeNull();
    }

    [Fact]
    public void TryParse_WithMissingSymbolField_ShouldReturnFalse()
    {
        var json = """
            {
              "stream": "btcusdt@trade",
              "data": {
                "e": "trade",
                "E": 1672515782136,
                "t": 12345,
                "p": "67321.11",
                "q": "0.250",
                "T": 1672515782136
              }
            }
            """;

        var result = _parser.TryParse(json, out var tradeTick);

        result.Should().BeFalse();
        tradeTick.Should().BeNull();
    }

    [Fact]
    public void TryParse_WithMissingPriceField_ShouldReturnFalse()
    {
        var json = """
            {
              "stream": "btcusdt@trade",
              "data": {
                "e": "trade",
                "E": 1672515782136,
                "s": "BTCUSDT",
                "t": 12345,
                "q": "0.250",
                "T": 1672515782136
              }
            }
            """;

        var result = _parser.TryParse(json, out var tradeTick);

        result.Should().BeFalse();
        tradeTick.Should().BeNull();
    }

    [Fact]
    public void TryParse_WithMissingQuantityField_ShouldReturnFalse()
    {
        var json = """
            {
              "stream": "btcusdt@trade",
              "data": {
                "e": "trade",
                "E": 1672515782136,
                "s": "BTCUSDT",
                "t": 12345,
                "p": "67321.11",
                "T": 1672515782136
              }
            }
            """;

        var result = _parser.TryParse(json, out var tradeTick);

        result.Should().BeFalse();
        tradeTick.Should().BeNull();
    }

    [Fact]
    public void TryParse_WithMissingTimestampField_ShouldReturnFalse()
    {
        var json = """
            {
              "stream": "btcusdt@trade",
              "data": {
                "e": "trade",
                "E": 1672515782136,
                "s": "BTCUSDT",
                "t": 12345,
                "p": "67321.11",
                "q": "0.250"
              }
            }
            """;

        var result = _parser.TryParse(json, out var tradeTick);

        result.Should().BeFalse();
        tradeTick.Should().BeNull();
    }

    [Fact]
    public void TryParse_WithMissingTradeIdField_ShouldReturnFalse()
    {
        var json = """
            {
              "stream": "btcusdt@trade",
              "data": {
                "e": "trade",
                "E": 1672515782136,
                "s": "BTCUSDT",
                "p": "67321.11",
                "q": "0.250",
                "T": 1672515782136
              }
            }
            """;

        var result = _parser.TryParse(json, out var tradeTick);

        result.Should().BeFalse();
        tradeTick.Should().BeNull();
    }

    [Fact]
    public void TryParse_WithInvalidPriceString_ShouldReturnFalse()
    {
        var json = BuildTradeMessage("BTCUSDT", "INVALID_PRICE", "0.250", 12345L, 1672515782136L);

        var result = _parser.TryParse(json, out var tradeTick);

        result.Should().BeFalse();
        tradeTick.Should().BeNull();
    }

    [Fact]
    public void TryParse_WithNegativePrice_ShouldReturnFalse()
    {
        var json = BuildTradeMessage("BTCUSDT", "-67321.11", "0.250", 12345L, 1672515782136L);

        var result = _parser.TryParse(json, out var tradeTick);

        result.Should().BeFalse();
        tradeTick.Should().BeNull();
    }

    [Fact]
    public void TryParse_WithNegativeQuantity_ShouldReturnFalse()
    {
        var json = BuildTradeMessage("BTCUSDT", "67321.11", "-0.250", 12345L, 1672515782136L);

        var result = _parser.TryParse(json, out var tradeTick);

        result.Should().BeFalse();
        tradeTick.Should().BeNull();
    }

    [Fact]
    public void TryParse_WithEmptySymbol_ShouldReturnFalse()
    {
        var json = BuildTradeMessage("", "67321.11", "0.250", 12345L, 1672515782136L);

        var result = _parser.TryParse(json, out var tradeTick);

        result.Should().BeFalse();
        tradeTick.Should().BeNull();
    }

    [Fact]
    public void TryParse_WithZeroTradeId_ShouldReturnFalse()
    {
        var json = BuildTradeMessage("BTCUSDT", "67321.11", "0.250", 0L, 1672515782136L);

        var result = _parser.TryParse(json, out var tradeTick);

        result.Should().BeFalse();
        tradeTick.Should().BeNull();
    }

    [Fact]
    public void TryParse_WithInvalidJson_ShouldReturnFalse()
    {
        var result = _parser.TryParse("this is not json at all", out var tradeTick);

        result.Should().BeFalse();
        tradeTick.Should().BeNull();
    }

    [Fact]
    public void TryParse_WithEmptyJsonObject_ShouldReturnFalse()
    {
        var result = _parser.TryParse("{}", out var tradeTick);

        result.Should().BeFalse();
        tradeTick.Should().BeNull();
    }

    private static string ValidBtcTradeMessage() =>
        """
        {
          "stream": "btcusdt@trade",
          "data": {
            "e": "trade",
            "E": 1672515782136,
            "s": "BTCUSDT",
            "t": 12345,
            "p": "67321.11",
            "q": "0.250",
            "T": 1672515782136,
            "m": true,
            "M": false
          }
        }
        """;

    private static string BuildTradeMessage(
        string symbol, string price, string quantity, long tradeId, long timestampMs) =>
        $$"""
        {
          "stream": "btcusdt@trade",
          "data": {
            "e": "trade",
            "E": {{timestampMs}},
            "s": "{{symbol}}",
            "t": {{tradeId}},
            "p": "{{price}}",
            "q": "{{quantity}}",
            "T": {{timestampMs}},
            "m": true,
            "M": false
          }
        }
        """;
}
