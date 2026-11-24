using MineSenseSafety.Shared.Domain.Repositories;
using OperationsService.Domain.Model.Aggregates;

namespace OperationsService.Domain.Repositories;

public interface IMiningUnitRepository : IBaseRepository<MiningUnit>
{
    Task<MiningUnit?> FindByBusinessCodeAsync(string businessCode);
}
