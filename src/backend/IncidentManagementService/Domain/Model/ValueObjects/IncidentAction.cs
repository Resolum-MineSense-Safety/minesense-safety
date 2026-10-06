using MineSenseSafety.Shared.Domain.Model;

namespace IncidentManagementService.Domain.Model.ValueObjects;

/// <summary>
/// Auditable response taken by a supervisor on an incident.
/// Description and outcome are required so the incident record stays complete.
/// </summary>
public record IncidentAction
{
    public Guid SupervisorId { get; }
    public string Description { get; }
    public string Outcome { get; }
    public DateTimeOffset RegisteredAt { get; }

    public IncidentAction(Guid supervisorId, string description, string outcome, DateTimeOffset registeredAt)
    {
        if (supervisorId == Guid.Empty)
            throw new DomainException("An incident action must be registered by a supervisor.");
        if (string.IsNullOrWhiteSpace(description))
            throw new DomainException("The action description is required.");
        if (string.IsNullOrWhiteSpace(outcome))
            throw new DomainException("The action outcome is required.");

        SupervisorId = supervisorId;
        Description = description.Trim();
        Outcome = outcome.Trim();
        RegisteredAt = registeredAt;
    }
}
