namespace IncidentManagementService.Interfaces.REST.Resources;

public record IncidentEventResource(string Type, Guid? ActorId, DateTimeOffset OccurredAt, string Detail);
