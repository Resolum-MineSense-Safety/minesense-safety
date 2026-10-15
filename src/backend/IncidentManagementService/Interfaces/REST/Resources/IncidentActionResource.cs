namespace IncidentManagementService.Interfaces.REST.Resources;

public record IncidentActionResource(
    Guid Id,
    string ActionType,
    string Description,
    string Result,
    Guid PerformedBy,
    DateTimeOffset PerformedAt);
