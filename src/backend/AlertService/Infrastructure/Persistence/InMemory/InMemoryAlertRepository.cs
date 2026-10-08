using AlertService.Domain.Model.Aggregates;
using AlertService.Domain.Model.ValueObjects;
using AlertService.Domain.Repositories;
using MineSenseSafety.Shared.Infrastructure.Persistence.InMemory;

namespace AlertService.Infrastructure.Persistence.InMemory;

public class InMemoryAlertRepository : InMemoryRepository<Alert>, IAlertRepository
{
    public Task<IEnumerable<Alert>> FindByOperatorIdAsync(Guid operatorId) =>
        Task.FromResult<IEnumerable<Alert>>(
            Store.Values.Where(alert => alert.OperatorId == operatorId)
                .OrderByDescending(alert => alert.IssuedAt)
                .ToList());

    // A3 (Infrastructure): filter by status and order by IssuedAt ascending.
    public Task<IEnumerable<Alert>> FindByStatusAsync(AlertStatus status) =>
        throw new NotImplementedException("A3: pending implementation.");
}
