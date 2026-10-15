using System.Globalization;
using ReportPdfGenerator.Model;

namespace ReportPdfGenerator.Pdf;

/// <summary>Lays out a compliance report: header, summary indicators, sections and footer.</summary>
public static class ReportDocumentBuilder
{
    private static readonly CultureInfo Format = CultureInfo.InvariantCulture;

    public static byte[] Build(ReportRequest request, DateTimeOffset generatedAt)
    {
        var pdf = new PdfDocumentWriter();

        pdf.Title(request.Title);
        pdf.Paragraph($"Periodo: {request.PeriodFrom.ToString("dd/MM/yyyy", Format)} - {request.PeriodTo.ToString("dd/MM/yyyy", Format)}");
        pdf.Paragraph($"Ámbito: {request.Scope}");
        pdf.Paragraph($"Solicitado por: {request.RequestedBy} · Generado: {generatedAt.ToString("dd/MM/yyyy HH:mm 'UTC'", Format)}");
        pdf.Paragraph($"Identificador: {request.ReportId}", 8);

        pdf.Heading("Resumen del periodo");
        var summary = request.Summary;
        pdf.Table(["Indicador", "Valor"],
        [
            ["Detecciones de fatiga", summary.Detections.ToString(Format)],
            ["Alertas emitidas", summary.Alerts.ToString(Format)],
            ["Alertas críticas", summary.CriticalAlerts.ToString(Format)],
            ["Incidentes cerrados", summary.IncidentsClosed.ToString(Format)],
            ["Tiempo medio de atención (min)", summary.AverageAttentionMinutes?.ToString("0.0", Format) ?? "Sin datos"]
        ]);

        foreach (var section in request.Sections)
        {
            pdf.Heading(section.Heading);
            if (!string.IsNullOrWhiteSpace(section.Text))
                pdf.Paragraph(section.Text);
            if (section.Columns is { Count: > 0 } && section.Rows is not null)
            {
                if (section.Rows.Count == 0) pdf.Paragraph("Sin registros en el periodo.");
                else pdf.Table(section.Columns, section.Rows);
            }
        }

        pdf.Line(string.Empty, 10, spacingBefore: 10);
        pdf.Paragraph("Documento generado automáticamente por MineSense Safety. Las variaciones reportadas " +
                      "son observaciones del periodo y no constituyen prueba de accidentes evitados.", 8);

        return pdf.ToBytes();
    }
}
