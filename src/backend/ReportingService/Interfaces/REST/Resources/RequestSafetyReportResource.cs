using System.Text.Json.Serialization;

namespace ReportingService.Interfaces.REST.Resources;

/// <param name="RequestedBy">
/// User requesting the report. Optional when the caller is authenticated: the <c>sub</c> claim is
/// used instead.
/// </param>
public record RequestSafetyReportResource(
    [property: JsonRequired] DateTimeOffset From,
    [property: JsonRequired] DateTimeOffset To,
    string? SiteId,
    string? FleetId,
    [property: JsonRequired] string DefinitionCode,
    [property: JsonRequired] string Format,
    Guid? RequestedBy = null);
