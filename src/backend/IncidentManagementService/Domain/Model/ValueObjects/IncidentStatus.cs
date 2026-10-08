namespace IncidentManagementService.Domain.Model.ValueObjects;

/// <summary>Lifecycle stage of a fatigue incident.</summary>
public enum IncidentStatus
{
    Pending,
    Assigned,
    Escalated,
    Closed
}
