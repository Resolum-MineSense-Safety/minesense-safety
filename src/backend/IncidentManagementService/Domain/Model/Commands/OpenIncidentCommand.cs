using IncidentManagementService.Domain.Model.ValueObjects;

namespace IncidentManagementService.Domain.Model.Commands;

/// <param name="CabinWarningIssuedAt">When the cabin warned, if the alert was synchronized from the Edge.</param>
public record OpenIncidentCommand(
    Guid OperatorId,
    Guid AlertId,
    IncidentSeverity Severity,
    DateTimeOffset? CabinWarningIssuedAt = null);
