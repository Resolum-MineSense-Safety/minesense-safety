namespace IncidentManagementService.Domain.Model.Commands;

public record AssignIncidentCommand(Guid IncidentId, Guid SupervisorId);
