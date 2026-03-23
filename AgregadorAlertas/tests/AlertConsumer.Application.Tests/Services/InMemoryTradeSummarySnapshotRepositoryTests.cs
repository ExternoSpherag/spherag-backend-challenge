using AlertConsumer.Domain.Entities;
using AlertConsumer.Infrastructure.Persistence;
using Xunit;

namespace AlertConsumer.Application.Tests.Services;

public class InMemoryTradeSummarySnapshotRepositoryTests
{
    [Fact]
    public async Task SaveAsync_LoadsExistingSnapshots_BySymbol()
    {
        var repository = new InMemoryTradeSummarySnapshotRepository();
        var tradeSummary = CreateTradeSummary("BTCUSDT", 100m, new DateTimeOffset(2026, 3, 21, 10, 0, 0, TimeSpan.Zero));

        await repository.SaveAsync(tradeSummary, CancellationToken.None);

        var loaded = await repository.GetLastBySymbolAsync("BTCUSDT", CancellationToken.None);

        Assert.NotNull(loaded);
        Assert.Equal(tradeSummary.Symbol, loaded!.Symbol);
        Assert.Equal(tradeSummary.AveragePrice, loaded.AveragePrice);
        Assert.Equal(tradeSummary.TimeUtc, loaded.TimeUtc);
    }

    [Fact]
    public async Task GetLastBySymbolAsync_WhenNoSnapshotsExist_ReturnsNull()
    {
        var repository = new InMemoryTradeSummarySnapshotRepository();

        var loaded = await repository.GetLastBySymbolAsync("BTCUSDT", CancellationToken.None);

        Assert.Null(loaded);
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
