using Microsoft.Extensions.Logging;
using NSubstitute;
using PosicionesConsumer.Application.Abstractions;
using PosicionesConsumer.Application.UseCases;
using PosicionesConsumer.Domain.Entities;
using Xunit;

namespace PosicionesConsumer.Application.Tests.UseCases;

public class ProcessTradeSummaryUseCaseTests
{
    [Fact]
    public async Task ProcessAsync_WhenTradeSummaryIsValid_PersistsTradeSummary()
    {
        var repository = Substitute.For<ITradeSummaryRepository>();
        var logger = Substitute.For<ILogger<ProcessTradeSummaryUseCase>>();
        var useCase = new ProcessTradeSummaryUseCase(repository, logger);
        var tradeSummary = CreateTradeSummary();

        await useCase.ProcessAsync(tradeSummary, CancellationToken.None);

        await repository.Received(1).SaveAsync(tradeSummary, CancellationToken.None);
    }

    [Fact]
    public async Task ProcessAsync_WhenTradeSummaryHasInvalidWindow_ThrowsAndDoesNotPersist()
    {
        var repository = Substitute.For<ITradeSummaryRepository>();
        var logger = Substitute.For<ILogger<ProcessTradeSummaryUseCase>>();
        var useCase = new ProcessTradeSummaryUseCase(repository, logger);
        var tradeSummary = CreateTradeSummary() with
        {
            WindowEnd = DateTimeOffset.UtcNow.AddMinutes(-10)
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => useCase.ProcessAsync(tradeSummary, CancellationToken.None));

        await repository.DidNotReceive().SaveAsync(Arg.Any<TradeSummary>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_WhenTradeSummaryIsNull_ThrowsArgumentNullException()
    {
        var repository = Substitute.For<ITradeSummaryRepository>();
        var logger = Substitute.For<ILogger<ProcessTradeSummaryUseCase>>();
        var useCase = new ProcessTradeSummaryUseCase(repository, logger);

        await Assert.ThrowsAsync<ArgumentNullException>(() => useCase.ProcessAsync(null!, CancellationToken.None));
    }

    private static TradeSummary CreateTradeSummary()
    {
        var now = DateTimeOffset.UtcNow;

        return new TradeSummary
        {
            Symbol = "BTCUSDT",
            Count = 5,
            AveragePrice = 101_000m,
            TotalQuantity = 2.4m,
            TimeUtc = now,
            WindowStart = now.AddMinutes(-1),
            WindowEnd = now
        };
    }
}
