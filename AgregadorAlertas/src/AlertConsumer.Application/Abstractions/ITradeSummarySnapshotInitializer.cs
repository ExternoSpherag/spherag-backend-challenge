namespace AlertConsumer.Application.Abstractions;

public interface ITradeSummarySnapshotInitializer
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
}
