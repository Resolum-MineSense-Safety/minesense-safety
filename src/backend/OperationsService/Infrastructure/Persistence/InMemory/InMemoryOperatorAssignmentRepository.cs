using MineSenseSafety.Shared.Infrastructure.Persistence.InMemory;
using OperationsService.Domain.Model.Aggregates;
using OperationsService.Domain.Repositories;

namespace OperationsService.Infrastructure.Persistence.InMemory;

public class InMemoryOperatorAssignmentRepository : InMemoryRepository<OperatorAssignment>, IOperatorAssignmentRepository
{
    public Task<IEnumerable<OperatorAssignment>> FindByOperatorIdAsync(Guid operatorId) =>
        Task.FromResult<IEnumerable<OperatorAssignment>>(
            Store.Values.Where(assignment => assignment.OperatorId == operatorId)
                .OrderByDescending(assignment => assignment.ValidFrom)
                .ThenByDescending(assignment => assignment.CreatedAt)
                .ToList());

    public Task<IEnumerable<OperatorAssignment>> FindByVehicleIdAsync(Guid vehicleId) =>
        Task.FromResult<IEnumerable<OperatorAssignment>>(
            Store.Values.Where(assignment => assignment.VehicleId == vehicleId)
                .OrderByDescending(assignment => assignment.ValidFrom)
                .ToList());
}
