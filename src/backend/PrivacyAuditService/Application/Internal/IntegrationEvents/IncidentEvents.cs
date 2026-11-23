namespace PrivacyAuditService.Application.Internal.IntegrationEvents;

/// <summary>Fields of <c>IncidentOpened</c> (Incident Management) kept in the audit trail.</summary>
public record IncidentOpenedEvent(Guid IncidentId, Guid AlertId, string Severity, DateTimeOffset OpenedAt);

/// <summary>Fields of <c>IncidentClosed</c> (Incident Management) kept in the audit trail.</summary>
public record IncidentClosedEvent(Guid IncidentId, Guid? ClosedBy, int ActionCount, DateTimeOffset ClosedAt);
