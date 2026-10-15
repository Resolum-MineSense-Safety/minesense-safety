using IncidentManagementService.Domain.Model.ValueObjects;

namespace IncidentManagementService.Domain.Model.Commands;

public record OpenIncidentCommand(Guid OperatorId, Guid AlertId, IncidentSeverity Severity);
