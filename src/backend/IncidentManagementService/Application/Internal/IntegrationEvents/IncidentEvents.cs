namespace IncidentManagementService.Application.Internal.IntegrationEvents;

/// <summary>Published on <c>minesense.incidents.opened</c> when the control center receives a new incident.</summary>
public record IncidentOpenedEvent(Guid IncidentId, Guid OperatorId, Guid AlertId, string Severity, DateTimeOffset OpenedAt)
{
    public const string Type = "IncidentOpened";
}

/// <summary>Published on <c>minesense.incidents.closed</c> when an incident is closed with its resolution.</summary>
public record IncidentClosedEvent(
    Guid IncidentId,
    Guid OperatorId,
    Guid AlertId,
    string Severity,
    Guid? ClosedBy,
    string Resolution,
    int ActionCount,
    DateTimeOffset ClosedAt)
{
    public const string Type = "IncidentClosed";
}

/// <summary>Fields of <c>AlertIssued</c> (Alerts) that Incident Management needs.</summary>
public record AlertIssuedEvent(Guid AlertId, Guid OperatorId, string Severity, DateTimeOffset IssuedAt);
