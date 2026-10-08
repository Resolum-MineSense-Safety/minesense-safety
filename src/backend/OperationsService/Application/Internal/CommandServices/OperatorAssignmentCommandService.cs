using OperationsService.Domain.Model.Aggregates;
using OperationsService.Domain.Model.Commands;
using OperationsService.Domain.Repositories;
using OperationsService.Domain.Services;

namespace OperationsService.Application.Internal.CommandServices;

// Sprint 1 · T17 (Jhosep Argomedo): create, change vehicle and end operator assignments (US19). Remaining: admin view in src/frontend.
public class OperatorAssignmentCommandService(
    IOperatorAssignmentRepository assignmentRepository,
    OperationalStructureValidator structureValidator,
    TimeProvider timeProvider) : IOperatorAssignmentCommandService
{
    public async Task<OperatorAssignment> Handle(CreateAssignmentCommand command)
    {
        var assignment = new OperatorAssignment(command, timeProvider.GetUtcNow());

        // T15: only existing, active vehicles can be assigned.
        await structureValidator.EnsureActiveVehicleAsync(assignment.VehicleId);

        AssignmentConflictPolicy.EnsureNoConflict(assignment, await RelatedAssignmentsAsync(assignment));

        await assignmentRepository.AddAsync(assignment);
        return assignment;
    }

    public async Task<OperatorAssignment?> Handle(ChangeAssignedVehicleCommand command)
    {
        var current = await assignmentRepository.FindByIdAsync(command.AssignmentId);
        if (current is null) return null;

        await structureValidator.EnsureActiveVehicleAsync(command.NewVehicleId);

        var replacement = current.ProposeVehicleChange(command.NewVehicleId, command.ChangeDate, timeProvider.GetUtcNow());
        AssignmentConflictPolicy.EnsureNoConflict(replacement, await RelatedAssignmentsAsync(replacement), current.Id);

        // The superseded assignment is kept: it is the history of the previous context.
        current.Supersede(command.ChangeDate);
        await assignmentRepository.UpdateAsync(current);
        await assignmentRepository.AddAsync(replacement);
        return replacement;
    }

    public async Task<OperatorAssignment?> Handle(EndAssignmentCommand command)
    {
        var assignment = await assignmentRepository.FindByIdAsync(command.AssignmentId);
        if (assignment is null) return null;

        assignment.End(command.EndDate);
        await assignmentRepository.UpdateAsync(assignment);
        return assignment;
    }

    private async Task<IEnumerable<OperatorAssignment>> RelatedAssignmentsAsync(OperatorAssignment assignment)
    {
        var byOperator = await assignmentRepository.FindByOperatorIdAsync(assignment.OperatorId);
        var byVehicle = await assignmentRepository.FindByVehicleIdAsync(assignment.VehicleId);
        return byOperator.Concat(byVehicle).DistinctBy(other => other.Id).ToList();
    }
}
