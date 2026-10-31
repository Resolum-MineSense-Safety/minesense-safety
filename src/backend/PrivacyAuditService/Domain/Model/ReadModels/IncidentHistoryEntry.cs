namespace PrivacyAuditService.Domain.Model.ReadModels;

/// <summary>
/// Single step of an incident traceability timeline (US23).
/// Steps come from the access audit log and from the IncidentOpened / IncidentClosed events of
/// Incident Management (ADR-004). Assignment and action steps stay in Incident Management's own history.
/// </summary>
public record IncidentHistoryEntry(
    DateTimeOffset OccurredAt,
    string Kind,
    Guid Actor,
    string Summary);
