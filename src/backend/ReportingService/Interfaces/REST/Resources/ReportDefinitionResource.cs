namespace ReportingService.Interfaces.REST.Resources;

public record ReportDefinitionResource(
    Guid Id,
    string Code,
    string Name,
    string Version,
    IEnumerable<string> DataSources,
    bool IsActive);
