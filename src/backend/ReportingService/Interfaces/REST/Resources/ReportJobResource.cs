namespace ReportingService.Interfaces.REST.Resources;

public record ReportJobResource(
    Guid Id,
    DateTimeOffset From,
    DateTimeOffset To,
    string? SiteId,
    string? FleetId,
    string DefinitionCode,
    string DefinitionVersion,
    string Format,
    Guid RequestedBy,
    string Status,
    DateTimeOffset RequestedAt,
    DateTimeOffset? CompletedAt,
    string? FailureReason,
    string? DownloadUri);
