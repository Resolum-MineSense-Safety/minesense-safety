namespace OperationsService.Interfaces.REST.Resources;

public record OperatorAssignmentResource(Guid Id, Guid OperatorId, Guid VehicleId, string Shift, DateOnly ValidFrom, DateOnly? ValidTo, string Status, DateTimeOffset CreatedAt);
