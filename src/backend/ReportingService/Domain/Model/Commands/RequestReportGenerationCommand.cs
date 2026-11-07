using ReportingService.Domain.Model.ValueObjects;

namespace ReportingService.Domain.Model.Commands;

public record RequestReportGenerationCommand(
    DateTimeOffset From,
    DateTimeOffset To,
    string? SiteId,
    string? FleetId,
    string DefinitionCode,
    ReportFormat Format,
    Guid RequestedBy);
