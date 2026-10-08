namespace OperationsService.Interfaces.REST.Resources;

public record CreateAssignmentResource(Guid OperatorId, Guid VehicleId, string? Shift, DateOnly ValidFrom, DateOnly? ValidTo);
