using SpheragBackendChallenge.Application.DTOs;
using SpheragBackendChallenge.Application.Interfaces;
using SpheragBackendChallenge.Application.Results;
using SpheragBackendChallenge.Application.UseCases;
using SpheragBackendChallenge.Domain.Entities;

namespace SpheragBackendChallenge.UnitTests.Application;

public sealed class PriceUseCaseTests
{
    [Fact]
    public async Task PriceUseCase_ReturnsValidationError_WhenFromIsGreaterThanTo()
    {
        var useCase = new PriceUseCase(new InMemoryTradeAggregationRepository());

        var result = await useCase.GetPricesAsync(
            new SymbolDateRangeDto
            {
                From = new DateTime(2026, 1, 1, 12, 0, 10, DateTimeKind.Utc),
                To = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc)
            },
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.Equal(OperationErrorType.Validation, result.Error!.Type);
    }

    [Fact]
    public async Task PriceUseCase_ReturnsSuccess_WithEmptyList_WhenNoPricesMatch()
    {
        var useCase = new PriceUseCase(new InMemoryTradeAggregationRepository());

        var result = await useCase.GetPricesAsync(new SymbolDateRangeDto(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Empty(result.Value!);
    }

    private sealed class InMemoryTradeAggregationRepository : ITradeAggregationRepository
    {
        public Task AddAsync(AggregatedPrice aggregatedPrice, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task<AggregatedPrice?> GetLatestBeforeWindowAsync(string symbol, DateTime windowStartUtc, CancellationToken cancellationToken)
        {
            return Task.FromResult<AggregatedPrice?>(null);
        }

        public Task<IReadOnlyList<AggregatedPrice>> QueryAsync(string? symbol, DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken)
        {
            IReadOnlyList<AggregatedPrice> result = [];
            return Task.FromResult(result);
        }
    }
}
