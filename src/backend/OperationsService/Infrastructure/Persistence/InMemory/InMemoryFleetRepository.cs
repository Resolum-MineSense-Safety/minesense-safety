using MineSenseSafety.Shared.Infrastructure.Persistence.InMemory;
using OperationsService.Domain.Model.Aggregates;
using OperationsService.Domain.Repositories;

namespace OperationsService.Infrastructure.Persistence.InMemory;

public class InMemoryFleetRepository : InMemoryRepository<Fleet>, IFleetRepository
{
    public Task<IEnumerable<Fleet>> FindByMiningUnitIdAsync(Guid miningUnitId) =>
        Task.FromResult<IEnumerable<Fleet>>(
            Store.Values.Where(fleet => fleet.MiningUnitId == miningUnitId)
                .OrderBy(fleet => fleet.Name)
                .ToList());
}
