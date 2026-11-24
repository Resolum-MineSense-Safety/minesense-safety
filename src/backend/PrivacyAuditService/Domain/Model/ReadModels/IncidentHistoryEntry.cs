namespace PrivacyAuditService.Domain.Model.ReadModels;

/// <summary>
/// Single step of an incident traceability timeline (US23).
/// Fase 1 slice: steps come from the access audit log; detection/alert/assignment/action
/// lifecycle events arrive with Fase 3 (Kafka) once producer contexts emit them.
/// </summary>
public record IncidentHistoryEntry(
    DateTimeOffset OccurredAt,
    string Kind,
    Guid Actor,
    string Summary);
