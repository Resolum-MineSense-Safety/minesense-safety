using System.Text.Json.Serialization;

namespace PrivacyAuditService.Interfaces.REST.Resources;

public record RequestSafetyReportResource(
    [property: JsonRequired] DateTimeOffset From,
    [property: JsonRequired] DateTimeOffset To,
    string? SiteId,
    string? FleetId);
