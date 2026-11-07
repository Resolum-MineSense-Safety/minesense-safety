using MineSenseSafety.Shared.Infrastructure.Persistence.InMemory;
using OperationsService.Domain.Model.Aggregates;
using OperationsService.Domain.Repositories;

namespace OperationsService.Infrastructure.Persistence.InMemory;

public class InMemoryOperatorRepository : InMemoryRepository<Operator>, IOperatorRepository
{
    public Task<Operator?> FindByEmployeeCodeAsync(string employeeCode) =>
        Task.FromResult(Store.Values.FirstOrDefault(@operator =>
            string.Equals(@operator.EmployeeCode, employeeCode, StringComparison.OrdinalIgnoreCase)));

    public Task<IEnumerable<Operator>> FindByMiningUnitIdAsync(Guid miningUnitId) =>
        Task.FromResult<IEnumerable<Operator>>(
            Store.Values.Where(@operator => @operator.MiningUnitId == miningUnitId)
                .OrderBy(@operator => @operator.EmployeeCode)
                .ToList());
}
