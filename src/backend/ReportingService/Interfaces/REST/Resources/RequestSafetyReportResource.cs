using System.Text.Json.Serialization;

namespace ReportingService.Interfaces.REST.Resources;

/// <param name="RequestedBy">
/// User requesting the report, for unauthenticated callers. When the request carries a token its
/// <c>sub</c> claim is used and this value is ignored.
/// </param>
public record RequestSafetyReportResource(
    [property: JsonRequired] DateTimeOffset From,
    [property: JsonRequired] DateTimeOffset To,
    string? SiteId,
    string? FleetId,
    [property: JsonRequired] string DefinitionCode,
    [property: JsonRequired] string Format,
    Guid? RequestedBy = null);
