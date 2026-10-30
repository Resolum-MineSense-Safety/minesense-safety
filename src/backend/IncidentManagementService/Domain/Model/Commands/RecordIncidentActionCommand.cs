using IncidentManagementService.Domain.Model.ValueObjects;

namespace IncidentManagementService.Domain.Model.Commands;

public record RecordIncidentActionCommand(
    Guid IncidentId,
    IncidentActionType ActionType,
    string Description,
    string Result,
    Guid PerformedBy,
    DateTimeOffset PerformedAt);
