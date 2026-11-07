namespace PrivacyAuditService.Domain.Model.Commands;

public record RequestSafetyReportCommand(
    DateTimeOffset From,
    DateTimeOffset To,
    string? SiteId,
    string? FleetId,
    Guid RequestedBy);
