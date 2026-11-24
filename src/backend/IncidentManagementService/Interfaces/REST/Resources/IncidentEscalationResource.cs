namespace IncidentManagementService.Interfaces.REST.Resources;

/// <summary>
/// TS-19: Status is NotApplicable (non-critical incident), NotRequired (attended
/// in time or deadline not reached) or Escalated (level and notified roles).
/// </summary>
public record IncidentEscalationResource(
    Guid IncidentId,
    string Status,
    int Level,
    IEnumerable<string> NotifiedRoles,
    DateTimeOffset? LastEscalatedAt);
