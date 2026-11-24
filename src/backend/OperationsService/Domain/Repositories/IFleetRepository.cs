using MineSenseSafety.Shared.Domain.Repositories;
using OperationsService.Domain.Model.Aggregates;

namespace OperationsService.Domain.Repositories;

public interface IFleetRepository : IBaseRepository<Fleet>
{
    Task<IEnumerable<Fleet>> FindByMiningUnitIdAsync(Guid miningUnitId);
}
