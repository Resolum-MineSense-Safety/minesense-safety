using ReportingService.Domain.Model.Aggregates;
using ReportingService.Domain.Model.ValueObjects;

namespace ReportingService.Domain.Services;

/// <summary>
/// Renders a report job with its definition. The in-process renderer is used locally; in the
/// cloud the same port is meant to be fulfilled by the ReportPdfGenerator Lambda (ADR-008).
/// </summary>
public interface IReportRenderer
{
    RenderedReport Render(ReportJob job, ReportDefinition definition, DateTimeOffset generatedAt);
}
