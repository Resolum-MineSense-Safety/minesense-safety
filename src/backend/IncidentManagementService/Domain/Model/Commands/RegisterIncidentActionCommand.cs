namespace IncidentManagementService.Domain.Model.Commands;

public record RegisterIncidentActionCommand(Guid IncidentId, Guid SupervisorId, string Description, string Outcome);
