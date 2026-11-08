namespace ReportingService.Domain.Model.ValueObjects;

/// <summary>Output of a report renderer, before it is stored as a <c>ReportArtifact</c>.</summary>
public record RenderedReport(string FileName, string ContentType, byte[] Content);
