using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SpheragBackendChallenge.Application.Configuration;
using SpheragBackendChallenge.Application.Interfaces;
using SpheragBackendChallenge.Application.Services;
using SpheragBackendChallenge.Domain.Entities;
using SpheragBackendChallenge.Domain.Models;

namespace SpheragBackendChallenge.UnitTests.Application;

public sealed class TradeWindowProcessorTests
{
    [Fact]
    public async Task TradeWindowProcessor_AcceptsLateTradesWithinGracePeriod()
    {
        var builder = TradeWindowProcessorBuilder.Create();
        var tradeTimestampUtc = new DateTime(2026, 1, 1, 12, 0, 4, DateTimeKind.Utc);

        var accepted = builder.Processor.TryProcessTrade(
            new TradeEvent("BTCUSDT", 100m, 1m, tradeTimestampUtc, 1),
            new DateTime(2026, 1, 1, 12, 0, 9, DateTimeKind.Utc));

        var earlyFlushCount = await builder.Processor.FlushExpiredWindowsAsync(
            new DateTime(2026, 1, 1, 12, 0, 10, DateTimeKind.Utc),
            CancellationToken.None);

        var finalFlushCount = await builder.Processor.FlushExpiredWindowsAsync(
            new DateTime(2026, 1, 1, 12, 0, 11, DateTimeKind.Utc),
            CancellationToken.None);

        Assert.True(accepted);
        Assert.Equal(0, earlyFlushCount);
        Assert.Equal(1, finalFlushCount);
        Assert.Single(builder.TradeAggregationRepository.AggregatedPrices);
    }

    [Fact]
    public async Task TradeWindowProcessor_RejectsLateTradesOutsideGracePeriod()
    {
        var builder = TradeWindowProcessorBuilder.Create();
        var accepted = builder.Processor.TryProcessTrade(
            new TradeEvent(
                "BTCUSDT",
                100m,
                1m,
                new DateTime(2026, 1, 1, 12, 0, 4, DateTimeKind.Utc),
                1),
            new DateTime(2026, 1, 1, 12, 0, 11, DateTimeKind.Utc));

        var flushCount = await builder.Processor.FlushExpiredWindowsAsync(
            new DateTime(2026, 1, 1, 12, 0, 11, DateTimeKind.Utc),
            CancellationToken.None);

        Assert.False(accepted);
        Assert.Equal(0, flushCount);
        Assert.Empty(builder.TradeAggregationRepository.AggregatedPrices);
    }

    [Fact]
    public async Task TradeWindowProcessor_CreatesAlertWhenConsecutiveWindowsDifferByMoreThanFivePercent()
    {
        var builder = TradeWindowProcessorBuilder.Create();

        builder.Processor.TryProcessTrade(
            new TradeEvent("BTCUSDT", 100m, 1m, new DateTime(2026, 1, 1, 12, 0, 1, DateTimeKind.Utc), 1),
            new DateTime(2026, 1, 1, 12, 0, 1, DateTimeKind.Utc));

        await builder.Processor.FlushExpiredWindowsAsync(
            new DateTime(2026, 1, 1, 12, 0, 11, DateTimeKind.Utc),
            CancellationToken.None);

        builder.Processor.TryProcessTrade(
            new TradeEvent("BTCUSDT", 106m, 1m, new DateTime(2026, 1, 1, 12, 0, 6, DateTimeKind.Utc), 2),
            new DateTime(2026, 1, 1, 12, 0, 9, DateTimeKind.Utc));

        await builder.Processor.FlushExpiredWindowsAsync(
            new DateTime(2026, 1, 1, 12, 0, 16, DateTimeKind.Utc),
            CancellationToken.None);

        Assert.Single(builder.PriceAlertRepository.Alerts);
        Assert.Equal(6m, builder.PriceAlertRepository.Alerts[0].PercentageChange);
    }

    [Fact]
    public async Task TradeWindowProcessor_DoesNotAggregateDuplicateTradeIdWithinSameWindow()
    {
        var builder = TradeWindowProcessorBuilder.Create();

        var firstAccepted = builder.Processor.TryProcessTrade(
            new TradeEvent("BTCUSDT", 100m, 1m, new DateTime(2026, 1, 1, 12, 0, 1, DateTimeKind.Utc), 1),
            new DateTime(2026, 1, 1, 12, 0, 1, DateTimeKind.Utc));

        var duplicateAccepted = builder.Processor.TryProcessTrade(
            new TradeEvent("BTCUSDT", 100m, 1m, new DateTime(2026, 1, 1, 12, 0, 2, DateTimeKind.Utc), 1),
            new DateTime(2026, 1, 1, 12, 0, 2, DateTimeKind.Utc));

        await builder.Processor.FlushExpiredWindowsAsync(
            new DateTime(2026, 1, 1, 12, 0, 11, DateTimeKind.Utc),
            CancellationToken.None);

        var aggregated = Assert.Single(builder.TradeAggregationRepository.AggregatedPrices);
        Assert.True(firstAccepted);
        Assert.False(duplicateAccepted);
        Assert.Equal(1, aggregated.TradeCount);
        Assert.Equal(100m, aggregated.AveragePrice);
    }

    private sealed class TradeWindowProcessorBuilder
    {
        private TradeWindowProcessorBuilder(
            TradeWindowProcessor processor,
            InMemoryTradeAggregationRepository tradeAggregationRepository,
            InMemoryPriceAlertRepository priceAlertRepository)
        {
            Processor = processor;
            TradeAggregationRepository = tradeAggregationRepository;
            PriceAlertRepository = priceAlertRepository;
        }

        public TradeWindowProcessor Processor { get; }

        public InMemoryTradeAggregationRepository TradeAggregationRepository { get; }

        public InMemoryPriceAlertRepository PriceAlertRepository { get; }

        public static TradeWindowProcessorBuilder Create()
        {
            var tradeAggregationRepository = new InMemoryTradeAggregationRepository();
            var priceAlertRepository = new InMemoryPriceAlertRepository();

            var services = new ServiceCollection();
            services.AddSingleton<ITradeAggregationRepository>(tradeAggregationRepository);
            services.AddSingleton<IPriceAlertRepository>(priceAlertRepository);

            var rootProvider = services.BuildServiceProvider();

            var processor = new TradeWindowProcessor(
                rootProvider.GetRequiredService<IServiceScopeFactory>(),
                Options.Create(new TradeProcessingOptions
                {
                    WindowSizeSeconds = 5,
                    AllowedLatenessSeconds = 5,
                    FlushIntervalSeconds = 1,
                    AlertThresholdPercentage = 5
                }),
                NullLogger<TradeWindowProcessor>.Instance);

            return new TradeWindowProcessorBuilder(processor, tradeAggregationRepository, priceAlertRepository);
        }
    }

    private sealed class InMemoryTradeAggregationRepository : ITradeAggregationRepository
    {
        public List<AggregatedPrice> AggregatedPrices { get; } = [];

        public Task AddAsync(AggregatedPrice aggregatedPrice, CancellationToken cancellationToken)
        {
            AggregatedPrices.Add(aggregatedPrice);
            return Task.CompletedTask;
        }

        public Task<AggregatedPrice?> GetLatestBeforeWindowAsync(string symbol, DateTime windowStartUtc, CancellationToken cancellationToken)
        {
            var price = AggregatedPrices
                .Where(x => x.Symbol == symbol && x.WindowStartUtc < windowStartUtc)
                .OrderByDescending(x => x.WindowStartUtc)
                .FirstOrDefault();

            return Task.FromResult(price);
        }

        public Task<IReadOnlyList<AggregatedPrice>> QueryAsync(string? symbol, DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken)
        {
            IReadOnlyList<AggregatedPrice> result = AggregatedPrices;
            return Task.FromResult(result);
        }
    }

    private sealed class InMemoryPriceAlertRepository : IPriceAlertRepository
    {
        public List<PriceAlert> Alerts { get; } = [];

        public Task AddAsync(PriceAlert alert, CancellationToken cancellationToken)
        {
            Alerts.Add(alert);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<PriceAlert>> QueryAsync(string? symbol, DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken)
        {
            IReadOnlyList<PriceAlert> result = Alerts;
            return Task.FromResult(result);
        }
    }
}
