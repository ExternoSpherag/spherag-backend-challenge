using AlertConsumer.Application.Abstractions;
using AlertConsumer.Application.Services;
using AlertConsumer.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace AlertConsumer.Application.Tests.Services;

public class TradeSummarySnapshotInitializerTests
{
    [Fact]
    public async Task InitializeAsync_LoadsLatestSnapshotsIntoRepository()
    {
        var snapshotRepository = Substitute.For<ITradeSummarySnapshotRepository>();
        var bootstrapRepository = Substitute.For<ITradeSummarySnapshotBootstrapRepository>();
        var tradeSummaries = new[]
        {
            CreateTradeSummary("BTCUSDT", 100m, new DateTimeOffset(2026, 3, 21, 10, 0, 0, TimeSpan.Zero)),
            CreateTradeSummary("ETHUSDT", 200m, new DateTimeOffset(2026, 3, 21, 10, 0, 5, TimeSpan.Zero))
        };

        bootstrapRepository.GetLatestPerSymbolAsync(Arg.Any<CancellationToken>())
            .Returns(tradeSummaries);

        var sut = new TradeSummarySnapshotInitializer(
            snapshotRepository,
            bootstrapRepository,
            NullLogger<TradeSummarySnapshotInitializer>.Instance);

        await sut.InitializeAsync(CancellationToken.None);

        await snapshotRepository.Received(1).SaveAsync(tradeSummaries[0], Arg.Any<CancellationToken>());
        await snapshotRepository.Received(1).SaveAsync(tradeSummaries[1], Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InitializeAsync_WhenDatabaseHasNoSnapshots_DoesNothing()
    {
        var snapshotRepository = Substitute.For<ITradeSummarySnapshotRepository>();
        var bootstrapRepository = Substitute.For<ITradeSummarySnapshotBootstrapRepository>();

        bootstrapRepository.GetLatestPerSymbolAsync(Arg.Any<CancellationToken>())
            .Returns(Array.Empty<TradeSummary>());

        var sut = new TradeSummarySnapshotInitializer(
            snapshotRepository,
            bootstrapRepository,
            NullLogger<TradeSummarySnapshotInitializer>.Instance);

        await sut.InitializeAsync(CancellationToken.None);

        await snapshotRepository.DidNotReceive().SaveAsync(Arg.Any<TradeSummary>(), Arg.Any<CancellationToken>());
    }

    private static TradeSummary CreateTradeSummary(string symbol, decimal averagePrice, DateTimeOffset timeUtc) =>
        new()
        {
            Symbol = symbol,
            Count = 10,
            AveragePrice = averagePrice,
            TotalQuantity = 2m,
            TimeUtc = timeUtc,
            WindowStart = timeUtc.AddMinutes(-1),
            WindowEnd = timeUtc
        };
}
