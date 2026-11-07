namespace OperationsService.Interfaces.REST.Resources;

public record OperationalContextResource(bool Valid, Guid? AssignmentId, string? Reason);
