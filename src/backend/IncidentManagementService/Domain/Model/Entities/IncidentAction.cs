using IncidentManagementService.Domain.Model.ValueObjects;
using MineSenseSafety.Shared.Domain.Model;

namespace IncidentManagementService.Domain.Model.Entities;

/// <summary>
/// Response adopted by a supervisor during an incident (pause, contact, relief...).
/// An action without a result is incomplete and cannot justify the closure (US10).
/// </summary>
public class IncidentAction
{
    public Guid Id { get; } = Guid.NewGuid();
    public IncidentActionType ActionType { get; }
    public string Description { get; }
    public string Result { get; }
    public Guid PerformedBy { get; }
    public DateTimeOffset PerformedAt { get; }

    public IncidentAction(IncidentActionType actionType, string description, string result,
        Guid performedBy, DateTimeOffset performedAt)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new DomainException("An incident action must describe what was done.");
        if (string.IsNullOrWhiteSpace(result))
            throw new DomainException("An incident action must state its result.");
        if (performedBy == Guid.Empty)
            throw new DomainException("An incident action must identify who performed it.");

        ActionType = actionType;
        Description = description.Trim();
        Result = result.Trim();
        PerformedBy = performedBy;
        PerformedAt = performedAt;
    }
}
