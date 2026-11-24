using MineSenseSafety.Shared.Domain.Repositories;
using OperationsService.Domain.Model.Aggregates;

namespace OperationsService.Domain.Repositories;

public interface IOperatorAssignmentRepository : IBaseRepository<OperatorAssignment>
{
    Task<IEnumerable<OperatorAssignment>> FindByOperatorIdAsync(Guid operatorId);
    Task<IEnumerable<OperatorAssignment>> FindByVehicleIdAsync(Guid vehicleId);
}
