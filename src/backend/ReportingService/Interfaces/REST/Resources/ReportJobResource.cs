namespace ReportingService.Interfaces.REST.Resources;

public record ReportJobResource(
    Guid Id,
    DateTimeOffset From,
    DateTimeOffset To,
    string? SiteId,
    string? FleetId,
    string DefinitionCode,
    string Format,
    Guid RequestedBy,
    string Status,
    DateTimeOffset RequestedAt);
