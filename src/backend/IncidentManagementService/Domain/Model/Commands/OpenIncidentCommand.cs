namespace IncidentManagementService.Domain.Model.Commands;

public record OpenIncidentCommand(Guid AlertId, Guid OperatorId);
