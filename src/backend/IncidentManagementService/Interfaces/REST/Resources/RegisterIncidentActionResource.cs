namespace IncidentManagementService.Interfaces.REST.Resources;

public record RegisterIncidentActionResource(Guid SupervisorId, string Description, string Outcome);
