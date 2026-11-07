using MineSenseSafety.Shared.Infrastructure.Persistence.InMemory;
using OperationsService.Domain.Model.Aggregates;
using OperationsService.Domain.Repositories;

namespace OperationsService.Infrastructure.Persistence.InMemory;

public class InMemoryMiningUnitRepository : InMemoryRepository<MiningUnit>, IMiningUnitRepository
{
    public Task<MiningUnit?> FindByBusinessCodeAsync(string businessCode) =>
        Task.FromResult(Store.Values.FirstOrDefault(unit =>
            string.Equals(unit.BusinessCode, businessCode, StringComparison.OrdinalIgnoreCase)));
}
