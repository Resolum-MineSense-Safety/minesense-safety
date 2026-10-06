namespace IncidentManagementService.Interfaces.REST.Resources;

public record IncidentActionResource(Guid SupervisorId, string Description, string Outcome, DateTimeOffset RegisteredAt);
