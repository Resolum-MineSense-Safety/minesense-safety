namespace OperationsService.Domain.Model.Commands;

public record EndAssignmentCommand(Guid AssignmentId, DateOnly EndDate);
