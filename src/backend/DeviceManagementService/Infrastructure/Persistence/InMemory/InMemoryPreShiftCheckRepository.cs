using DeviceManagementService.Domain.Model.Aggregates;
using DeviceManagementService.Domain.Repositories;
using MineSenseSafety.Shared.Infrastructure.Persistence.InMemory;

namespace DeviceManagementService.Infrastructure.Persistence.InMemory;

public class InMemoryPreShiftCheckRepository : InMemoryRepository<PreShiftCheck>, IPreShiftCheckRepository
{
    public Task<PreShiftCheck?> FindLatestByOperatorIdAsync(Guid operatorId) =>
        Task.FromResult(Store.Values.Where(check => check.OperatorId == operatorId)
            .OrderByDescending(check => check.CheckedAt)
            .FirstOrDefault());
}
