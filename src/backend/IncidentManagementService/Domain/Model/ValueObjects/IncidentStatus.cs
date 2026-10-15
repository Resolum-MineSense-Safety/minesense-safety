namespace IncidentManagementService.Domain.Model.ValueObjects;

public enum IncidentStatus
{
    /// <summary>Received in the control center, nobody is responsible yet.</summary>
    Pending,

    /// <summary>A supervisor took ownership and is responding.</summary>
    Assigned,

    /// <summary>Resolved with at least one documented action.</summary>
    Closed
}
