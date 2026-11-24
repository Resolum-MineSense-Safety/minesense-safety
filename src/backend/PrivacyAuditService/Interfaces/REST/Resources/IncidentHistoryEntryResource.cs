namespace PrivacyAuditService.Interfaces.REST.Resources;

public record IncidentHistoryEntryResource(
    DateTimeOffset OccurredAt,
    string Kind,
    Guid Actor,
    string Summary);
