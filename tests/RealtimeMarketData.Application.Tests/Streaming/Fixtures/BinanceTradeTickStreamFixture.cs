using Microsoft.Extensions.Logging;
using Moq;
using RealtimeMarketData.Infrastructure.Streaming.Binance;
using Xunit;

namespace RealtimeMarketData.Application.Tests.Streaming.Fixtures;

public sealed class BinanceTradeTickStreamFixture : IAsyncLifetime
{
    public Mock<ILogger<BinanceTradeTickStream>> LoggerMock { get; }

    public BinanceTradeTickStreamFixture()
    {
        LoggerMock = new Mock<ILogger<BinanceTradeTickStream>>(MockBehavior.Loose);
    }

    public string GetValidBtcTradeMessage() =>
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

    public string GetValidEthTradeMessage() =>
        """
        {
          "stream": "ethusdt@trade",
          "data": {
            "e": "trade",
            "E": 1672515783000,
            "s": "ETHUSDT",
            "t": 54321,
            "p": "2450.50",
            "q": "1.500",
            "T": 1672515783000,
            "m": false,
            "M": false
          }
        }
        """;

    public string GetValidDogeTradeMessage() =>
        """
        {
          "stream": "dogeusdt@trade",
          "data": {
            "e": "trade",
            "E": 1672515784000,
            "s": "DOGEUSDT",
            "t": 99999,
            "p": "0.15",
            "q": "10000.00",
            "T": 1672515784000,
            "m": true,
            "M": false
          }
        }
        """;

    public string GetMalformedMessageMissingData() =>
        """
        {
          "stream": "btcusdt@trade"
        }
        """;

    public string GetMalformedMessageMissingEventType() =>
        """
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

    public string GetMalformedMessageInvalidPrice() =>
        """
        {
          "stream": "btcusdt@trade",
          "data": {
            "e": "trade",
            "E": 1672515782136,
            "s": "BTCUSDT",
            "t": 12345,
            "p": "INVALID_PRICE",
            "q": "0.250",
            "T": 1672515782136,
            "m": true,
            "M": false
          }
        }
        """;

    public string GetMalformedMessageNegativeQuantity() =>
        """
        {
          "stream": "btcusdt@trade",
          "data": {
            "e": "trade",
            "E": 1672515782136,
            "s": "BTCUSDT",
            "t": 12345,
            "p": "67321.11",
            "q": "-0.250",
            "T": 1672515782136,
            "m": true,
            "M": false
          }
        }
        """;

    public string GetNonTradeEventMessage() =>
        """
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

    public string GetMalformedMessageEmptySymbol() =>
        """
        {
          "stream": "btcusdt@trade",
          "data": {
            "e": "trade",
            "E": 1672515782136,
            "s": "",
            "t": 12345,
            "p": "67321.11",
            "q": "0.250",
            "T": 1672515782136,
            "m": true,
            "M": false
          }
        }
        """;

    public string GetMalformedMessageInvalidTradeId() =>
        """
        {
          "stream": "btcusdt@trade",
          "data": {
            "e": "trade",
            "E": 1672515782136,
            "s": "BTCUSDT",
            "t": 0,
            "p": "67321.11",
            "q": "0.250",
            "T": 1672515782136,
            "m": true,
            "M": false
          }
        }
        """;

    public string GetInvalidJsonMessage() => "this is not json at all";

    public string GetEmptyJsonMessage() => "{}";

    public int GetWarningLogCount() =>
        LoggerMock.Invocations.Count(i =>
            i.Method.Name == "Log" &&
            (LogLevel)i.Arguments[0] == LogLevel.Warning);

    public int GetErrorLogCount() =>
        LoggerMock.Invocations.Count(i =>
            i.Method.Name == "Log" &&
            (LogLevel)i.Arguments[0] == LogLevel.Error);

    public void Reset() => LoggerMock.Reset();

    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync() => Task.CompletedTask;
}
