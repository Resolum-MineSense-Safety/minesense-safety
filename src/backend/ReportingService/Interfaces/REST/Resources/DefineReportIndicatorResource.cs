using System.Text.Json.Serialization;

namespace ReportingService.Interfaces.REST.Resources;

public record DefineReportIndicatorResource(
    [property: JsonRequired] string Code,
    [property: JsonRequired] string Name,
    [property: JsonRequired] string Version,
    [property: JsonRequired] IEnumerable<string> DataSources);
