using OperationsService.Domain.Model.Aggregates;
using OperationsService.Domain.Model.Commands;

namespace OperationsService.Domain.Services;

public interface IOperatorAssignmentCommandService
{
    Task<OperatorAssignment> Handle(CreateAssignmentCommand command);
    /// <summary>Returns the new assignment created by the change, or null when the assignment does not exist.</summary>
    Task<OperatorAssignment?> Handle(ChangeAssignedVehicleCommand command);
    Task<OperatorAssignment?> Handle(EndAssignmentCommand command);
}
