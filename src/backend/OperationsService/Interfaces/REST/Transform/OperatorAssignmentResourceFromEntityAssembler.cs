using OperationsService.Domain.Model.Aggregates;
using OperationsService.Interfaces.REST.Resources;

namespace OperationsService.Interfaces.REST.Transform;

public static class OperatorAssignmentResourceFromEntityAssembler
{
    public static OperatorAssignmentResource ToResourceFromEntity(OperatorAssignment assignment) =>
        new(assignment.Id, assignment.OperatorId, assignment.VehicleId, assignment.Shift.ToString(),
            assignment.ValidFrom, assignment.ValidTo, assignment.Status.ToString(), assignment.CreatedAt);
}
