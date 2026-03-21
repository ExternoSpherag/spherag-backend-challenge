using System.Runtime.CompilerServices;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RealtimeMarketData.Api.Services;
using RealtimeMarketData.Application.Common.Results;
using RealtimeMarketData.Application.Features.Streaming;
using RealtimeMarketData.Application.Features.Streaming.Commands.IngestTradeTick;
using Xunit;

namespace RealtimeMarketData.Application.Tests.Services;

public sealed class TradeTickIngestionBackgroundServiceTests
{
    private readonly Mock<ITradeTickStream> _streamMock = new();
    private readonly Mock<IServiceScopeFactory> _scopeFactoryMock = new();
    private readonly Mock<ISender> _senderMock = new();
    private readonly TradeTickIngestionBackgroundService _service;

    public TradeTickIngestionBackgroundServiceTests()
    {
        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider
            .Setup(sp => sp.GetService(typeof(ISender)))
            .Returns(_senderMock.Object);

        var scope = new Mock<IServiceScope>();
        scope.Setup(s => s.ServiceProvider).Returns(serviceProvider.Object);

        _scopeFactoryMock
            .Setup(f => f.CreateScope())
            .Returns(scope.Object);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MarketData:Ingestion:ChannelCapacity"] = "500"
            })
            .Build();

        _service = new TradeTickIngestionBackgroundService(
            _streamMock.Object,
            _scopeFactoryMock.Object,
            configuration,
            NullLogger<TradeTickIngestionBackgroundService>.Instance);
    }

    private static TradeTick BuildTick(long tradeId) =>
        new("BTCUSDT", 50_000m, 1m, DateTimeOffset.UtcNow, tradeId);

    private static async IAsyncEnumerable<TradeTick> FiniteStream(
        IEnumerable<TradeTick> ticks,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var tick in ticks)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return tick;
            await Task.Yield();
        }
    }

    private static async IAsyncEnumerable<TradeTick> InfiniteStream(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var i = 0L;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return new TradeTick("BTCUSDT", 50_000m, 1m, DateTimeOffset.UtcNow, ++i);
            await Task.Yield();
        }
    }

    [Fact]
    public async Task ExecuteAsync_WithFiniteStream_ShouldDispatchOneCommandPerTick()
    {
        var ticks = Enumerable.Range(1, 3).Select(i => BuildTick(i)).ToList();

        _streamMock
            .Setup(s => s.ReadAllAsync(It.IsAny<CancellationToken>()))
            .Returns((CancellationToken ct) => FiniteStream(ticks, ct));

        _senderMock
            .Setup(s => s.Send(It.IsAny<IngestTradeTickCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        await _service.StartAsync(CancellationToken.None);
        await _service.ExecuteTask!.WaitAsync(TimeSpan.FromSeconds(5));

        _senderMock.Verify(
            s => s.Send(It.IsAny<IngestTradeTickCommand>(), It.IsAny<CancellationToken>()),
            Times.Exactly(3));
    }

    [Fact]
    public async Task ExecuteAsync_WhenOneTickProcessingThrows_ShouldContinueWithRemainingTicks()
    {
        var ticks = Enumerable.Range(1, 3).Select(i => BuildTick(i)).ToList();

        _streamMock
            .Setup(s => s.ReadAllAsync(It.IsAny<CancellationToken>()))
            .Returns((CancellationToken ct) => FiniteStream(ticks, ct));

        _senderMock
            .SetupSequence(s => s.Send(It.IsAny<IngestTradeTickCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("processing error"))
            .ReturnsAsync(Result.Success())
            .ReturnsAsync(Result.Success());

        await _service.StartAsync(CancellationToken.None);
        await _service.ExecuteTask!.WaitAsync(TimeSpan.FromSeconds(5));

        _senderMock.Verify(
            s => s.Send(It.IsAny<IngestTradeTickCommand>(), It.IsAny<CancellationToken>()),
            Times.Exactly(3));
    }

    [Fact]
    public async Task ExecuteAsync_WhenCancelled_ShouldCompleteWithoutException()
    {
        using var cts = new CancellationTokenSource();

        _streamMock
            .Setup(s => s.ReadAllAsync(It.IsAny<CancellationToken>()))
            .Returns((CancellationToken ct) => InfiniteStream(ct));

        _senderMock
            .Setup(s => s.Send(It.IsAny<IngestTradeTickCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        await _service.StartAsync(cts.Token);
        await cts.CancelAsync();

        var act = async () => await _service.ExecuteTask!.WaitAsync(TimeSpan.FromSeconds(5));

        await act.Should().NotThrowAsync();
    }
}