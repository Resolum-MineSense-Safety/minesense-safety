namespace IncidentManagementService.Interfaces.REST.Resources;

public record IncidentResource(
    Guid Id,
    Guid AlertId,
    Guid OperatorId,
    string Status,
    DateTimeOffset OpenedAt,
    Guid? AssignedSupervisorId,
    DateTimeOffset? AssignedAt,
    IEnumerable<IncidentActionResource> Actions,
    string? Resolution,
    DateTimeOffset? ClosedAt);
