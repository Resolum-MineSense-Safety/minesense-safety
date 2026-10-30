namespace PrivacyAuditService.Interfaces.REST.Resources;

public record ReportResource(
    Guid Id,
    DateTimeOffset From,
    DateTimeOffset To,
    string? SiteId,
    string? FleetId,
    Guid RequestedBy,
    string Status,
    DateTimeOffset RequestedAt);
