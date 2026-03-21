using System.Runtime.CompilerServices;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using RealtimeMarketData.Application.Features.Streaming;
using RealtimeMarketData.Application.Tests.Streaming.Fixtures;
using RealtimeMarketData.Infrastructure.Streaming.Binance;
using Xunit;

namespace RealtimeMarketData.Application.Tests.Streaming;

public sealed class BinanceTradeTickStreamTests : IClassFixture<BinanceTradeTickStreamFixture>
{
    private readonly BinanceTradeTickStreamFixture _fixture;

    public BinanceTradeTickStreamTests(BinanceTradeTickStreamFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ReadAllAsync_WithValidBtcMessage_ShouldYieldCorrectTradeTick()
    {
        _fixture.Reset();
        await using var stream = CreateStub([_fixture.GetValidBtcTradeMessage()]);

        var ticks = await stream.ReadAllAsync().ToListAsync();

        ticks.Should().HaveCount(1);
        ticks[0].Symbol.Should().Be("BTCUSDT");
        ticks[0].Price.Should().Be(67321.11m);
        ticks[0].Quantity.Should().Be(0.250m);
        ticks[0].TradeId.Should().Be(12345L);
        ticks[0].TradeTimestamp.ToUnixTimeMilliseconds().Should().Be(1672515782136L);
    }

    [Theory]
    [InlineData(nameof(BinanceTradeTickStreamFixture.GetValidEthTradeMessage), "ETHUSDT", 54321L)]
    [InlineData(nameof(BinanceTradeTickStreamFixture.GetValidDogeTradeMessage), "DOGEUSDT", 99999L)]
    public async Task ReadAllAsync_WithValidAltcoinMessage_ShouldYieldCorrectTick(
        string messageMethodName,
        string expectedSymbol,
        long expectedTradeId)
    {
        _fixture.Reset();
        var method = typeof(BinanceTradeTickStreamFixture).GetMethod(messageMethodName);
        var message = (string)method!.Invoke(_fixture, null)!;
        await using var stream = CreateStub([message]);

        var ticks = await stream.ReadAllAsync().ToListAsync();

        ticks.Should().HaveCount(1);
        ticks[0].Symbol.Should().Be(expectedSymbol);
        ticks[0].TradeId.Should().Be(expectedTradeId);
    }

    [Fact]
    public async Task ReadAllAsync_WithMultipleValidMessages_ShouldYieldAllTradeTicks()
    {
        _fixture.Reset();
        await using var stream = CreateStub([
            _fixture.GetValidBtcTradeMessage(),
            _fixture.GetValidEthTradeMessage(),
            _fixture.GetValidDogeTradeMessage()]);

        var ticks = await stream.ReadAllAsync().ToListAsync();

        ticks.Should().HaveCount(3);
        ticks.Select(t => t.Symbol).Should().BeEquivalentTo(["BTCUSDT", "ETHUSDT", "DOGEUSDT"]);
    }

    [Fact]
    public async Task ReadAllAsync_WithInvalidMessage_ShouldSkipTickAndLogWarning()
    {
        _fixture.Reset();
        await using var stream = CreateStub([_fixture.GetMalformedMessageMissingData()]);

        var ticks = await stream.ReadAllAsync().ToListAsync();

        ticks.Should().BeEmpty();
        _fixture.GetWarningLogCount().Should().Be(1);
    }

    [Fact]
    public async Task ReadAllAsync_WithNonTradeEvent_ShouldSkipTickAndLogWarning()
    {
        _fixture.Reset();
        await using var stream = CreateStub([_fixture.GetNonTradeEventMessage()]);

        var ticks = await stream.ReadAllAsync().ToListAsync();

        ticks.Should().BeEmpty();
        _fixture.GetWarningLogCount().Should().Be(1);
    }

    [Fact]
    public async Task ReadAllAsync_WithMixedMessages_ShouldYieldOnlyValidTicksAndLogWarnings()
    {
        _fixture.Reset();
        await using var stream = CreateStub([
            _fixture.GetValidBtcTradeMessage(),
            _fixture.GetMalformedMessageMissingData(),
            _fixture.GetValidEthTradeMessage(),
            _fixture.GetNonTradeEventMessage()]);

        var ticks = await stream.ReadAllAsync().ToListAsync();

        ticks.Should().HaveCount(2);
        ticks.Select(t => t.Symbol).Should().BeEquivalentTo(["BTCUSDT", "ETHUSDT"]);
        _fixture.GetWarningLogCount().Should().Be(2);
    }

    [Fact]
    public async Task ReadAllAsync_WhenCancelledBeforeIteration_ShouldNotYieldAnyTick()
    {
        _fixture.Reset();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        await using var stream = CreateStub([_fixture.GetValidBtcTradeMessage()]);

        var ticks = new List<TradeTick>();
        try
        {
            await foreach (var tick in stream.ReadAllAsync(cts.Token))
            {
                ticks.Add(tick);
            }
        }
        catch (OperationCanceledException) { }

        ticks.Should().BeEmpty();
    }

    private StubBinanceTradeTickStream CreateStub(IEnumerable<string> messages) =>
        new(_fixture.LoggerMock.Object, messages);

    private sealed class StubBinanceTradeTickStream(
        ILogger<BinanceTradeTickStream> logger,
        IEnumerable<string> messages) : BinanceTradeTickStream(logger)
    {
        private readonly IReadOnlyList<string> _messages = messages.ToList();

        protected override async IAsyncEnumerable<string> ReadTextMessagesAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            foreach (var msg in _messages)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return msg;
                await Task.Yield();
            }
        }
    }
}

