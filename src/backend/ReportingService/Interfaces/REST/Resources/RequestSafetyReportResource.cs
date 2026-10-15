using System.Text.Json.Serialization;

namespace ReportingService.Interfaces.REST.Resources;

public record RequestSafetyReportResource(
    [property: JsonRequired] DateTimeOffset From,
    [property: JsonRequired] DateTimeOffset To,
    string? SiteId,
    string? FleetId,
    [property: JsonRequired] string DefinitionCode,
    [property: JsonRequired] string Format);
