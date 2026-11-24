namespace IncidentManagementService.Domain.Model.ValueObjects;

/// <summary>Milestones kept in the incident history (TS-27).</summary>
public enum IncidentEventType
{
    Opened,
    Assigned,
    ActionRecorded,
    Escalated,
    Closed
}
