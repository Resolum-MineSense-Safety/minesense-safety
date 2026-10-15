namespace IncidentManagementService.Interfaces.REST.Resources;

public record IncidentResource(
    Guid Id,
    Guid OperatorId,
    Guid AlertId,
    string Severity,
    string Status,
    DateTimeOffset OpenedAt,
    Guid? AssigneeId,
    DateTimeOffset? AssignedAt,
    int EscalationLevel,
    string? Resolution,
    DateTimeOffset? ClosedAt,
    double? AttentionTimeSeconds,
    IEnumerable<IncidentActionResource> Actions);
