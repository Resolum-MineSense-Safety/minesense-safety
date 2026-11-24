namespace OperationsService.Domain.Model.Queries;

public record GetActiveAssignmentByOperatorQuery(Guid OperatorId, DateOnly Date);
