using System.Text.Json;
using ReportingService.Domain.Model.Aggregates;

namespace ReportingService.Application.Internal.Serverless;

/// <summary>
/// Body of the report-requests message: the <c>ReportRequest</c> contract the ReportPdfGenerator Lambda
/// validates (a contract test of ReportingService.Tests checks it against the Lambda's own classes).
/// Consolidated figures arrive with the Kafka read models (ADR-004); until then the summary goes empty,
/// which the PDF prints as "Sin datos" rather than as zero alerts.
/// </summary>
public record ReportRequestMessage(
    Guid ReportId,
    string Title,
    DateTimeOffset PeriodFrom,
    DateTimeOffset PeriodTo,
    string RequestedBy,
    string Scope,
    ReportRequestSummary Summary,
    IReadOnlyList<ReportRequestSection> Sections,
    DateTimeOffset RequestedAt)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static ReportRequestMessage From(ReportJob job, ReportDefinition definition) => new(
        job.Id,
        $"{definition.Name} ({definition.Version})",
        job.From,
        job.To,
        job.RequestedBy.ToString(),
        $"Sitio: {job.SiteId ?? "todos"} - Flota: {job.FleetId ?? "todas"}",
        new ReportRequestSummary(null, null, null, null, null),
        [
            new ReportRequestSection(
                "Indicador",
                "Las cifras consolidadas se incorporarán cuando Reporting Service reciba los eventos de los demás contextos.",
                ["Campo", "Valor"],
                [
                    ["Código", definition.Code],
                    ["Versión", definition.Version],
                    ["Fuentes de datos", string.Join(" ", definition.DataSources)]
                ])
        ],
        job.RequestedAt);

    public static string ToJson(ReportJob job, ReportDefinition definition) =>
        JsonSerializer.Serialize(From(job, definition), JsonOptions);
}

public record ReportRequestSummary(
    int? Detections,
    int? Alerts,
    int? CriticalAlerts,
    int? IncidentsClosed,
    double? AverageAttentionMinutes);

public record ReportRequestSection(
    string Heading,
    string? Text,
    IReadOnlyList<string>? Columns,
    IReadOnlyList<IReadOnlyList<string>>? Rows);
