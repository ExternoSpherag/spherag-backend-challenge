using AlertConsumer.Domain.Entities;

namespace AlertConsumer.Application.Abstractions;

public interface IPriceAlertRepository
{
    Task AddAsync(PriceAlert alert, CancellationToken cancellationToken = default);
}

