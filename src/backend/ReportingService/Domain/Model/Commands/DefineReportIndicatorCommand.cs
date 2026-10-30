namespace ReportingService.Domain.Model.Commands;

public record DefineReportIndicatorCommand(
    string Code,
    string Name,
    string Version,
    IEnumerable<string> DataSources);
