using OperationsService.Domain.Model.Aggregates;
using OperationsService.Domain.Model.Queries;
using OperationsService.Domain.Model.ValueObjects;
using OperationsService.Domain.Repositories;
using OperationsService.Domain.Services;

namespace OperationsService.Application.Internal.QueryServices;

// Sprint 1 · T18 (Andreow Santiago): Handle(ValidateOperationalContextQuery) validates operator + vehicle + date before a
// monitoring session starts; consumed by the Monitoring service. Remaining: check the shift against the session start time.
public class OperatorAssignmentQueryService(
    IOperatorAssignmentRepository assignmentRepository,
    IVehicleRepository vehicleRepository) : IOperatorAssignmentQueryService
{
    public Task<OperatorAssignment?> Handle(GetAssignmentByIdQuery query) =>
        assignmentRepository.FindByIdAsync(query.AssignmentId);

    public async Task<OperatorAssignment?> Handle(GetActiveAssignmentByOperatorQuery query) =>
        (await assignmentRepository.FindByOperatorIdAsync(query.OperatorId))
        .FirstOrDefault(assignment => assignment.IsActive && assignment.Covers(query.Date));

    public Task<IEnumerable<OperatorAssignment>> Handle(GetAssignmentHistoryByOperatorQuery query) =>
        assignmentRepository.FindByOperatorIdAsync(query.OperatorId);

    public async Task<OperationalContextValidation> Handle(ValidateOperationalContextQuery query)
    {
        if (string.IsNullOrWhiteSpace(query.VehicleCode))
            return OperationalContextValidation.Rejected("Vehicle code is required.");

        var vehicle = await vehicleRepository.FindByCodeAsync(Vehicle.NormalizeCode(query.VehicleCode));
        if (vehicle is null)
            return OperationalContextValidation.Rejected($"Vehicle {Vehicle.NormalizeCode(query.VehicleCode)} is not registered.");
        if (!vehicle.IsActive)
            return OperationalContextValidation.Rejected($"Vehicle {vehicle.Code} is {vehicle.Status}.");

        var assignment = await Handle(new GetActiveAssignmentByOperatorQuery(query.OperatorId, query.Date));
        if (assignment is null)
            return OperationalContextValidation.Rejected(
                $"Operator {query.OperatorId} has no active assignment on {query.Date:yyyy-MM-dd}.");
        if (assignment.VehicleId != vehicle.Id)
            return OperationalContextValidation.Rejected(
                $"Operator {query.OperatorId} is assigned to another vehicle on {query.Date:yyyy-MM-dd}.");

        return OperationalContextValidation.Accepted(assignment.Id);
    }
}
