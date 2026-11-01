namespace ReportPdfGenerator.Model;

/// <summary>
/// Message the Reporting Service puts in the report-requests queue. It already
/// carries the consolidated data (alerts, incidents, trends), so the function
/// only renders and stores the document. <c>RequestedAt</c> fixes the storage key so the service can
/// find the PDF knowing only the job (without it the key uses the processing date).
/// </summary>
public record ReportRequest(
    Guid ReportId,
    string Title,
    DateTimeOffset PeriodFrom,
    DateTimeOffset PeriodTo,
    string RequestedBy,
    string Scope,
    ReportSummary Summary,
    IReadOnlyList<ReportSection> Sections,
    DateTimeOffset? RequestedAt = null);

/// <summary>Headline indicators of the period (EP10); null means not consolidated, never zero.</summary>
public record ReportSummary(
    int? Detections,
    int? Alerts,
    int? CriticalAlerts,
    int? IncidentsClosed,
    double? AverageAttentionMinutes);

/// <summary>A titled block of the report: a short text and/or a table.</summary>
public record ReportSection(
    string Heading,
    string? Text,
    IReadOnlyList<string>? Columns,
    IReadOnlyList<IReadOnlyList<string>>? Rows);
