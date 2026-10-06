namespace IncidentManagementService.Domain.Model.Commands;

public record CloseIncidentCommand(Guid IncidentId, string Resolution);
