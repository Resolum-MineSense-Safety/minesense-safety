using MineSenseSafety.Shared.Domain.Repositories;
using OperationsService.Domain.Model.Aggregates;

namespace OperationsService.Domain.Repositories;

public interface IOperatorRepository : IBaseRepository<Operator>
{
    Task<Operator?> FindByEmployeeCodeAsync(string employeeCode);
    Task<IEnumerable<Operator>> FindByMiningUnitIdAsync(Guid miningUnitId);
}
