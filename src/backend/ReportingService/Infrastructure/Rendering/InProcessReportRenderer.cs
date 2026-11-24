using System.Globalization;
using System.Text;
using ReportingService.Domain.Model.Aggregates;
using ReportingService.Domain.Model.ValueObjects;
using ReportingService.Domain.Services;

namespace ReportingService.Infrastructure.Rendering;

/// <summary>
/// Renders the report in process (CSV or a one-page PDF) with the job parameters and the indicator
/// definition. Consolidated figures from the producer contexts arrive with the Kafka read models
/// (ADR-004); until then the document lists the scope and data sources that will be aggregated.
/// </summary>
public class InProcessReportRenderer : IReportRenderer
{
    public RenderedReport Render(ReportJob job, ReportDefinition definition, DateTimeOffset generatedAt)
    {
        var rows = Rows(job, definition, generatedAt);
        var baseName = $"{job.DefinitionCode}-{job.Id:N}";

        return job.Format switch
        {
            ReportFormat.Csv => new RenderedReport($"{baseName}.csv", "text/csv", Csv(rows)),
            ReportFormat.Pdf => new RenderedReport($"{baseName}.pdf", "application/pdf",
                MinimalPdf.Render($"{definition.Name} ({definition.Version})",
                    rows.Select(row => $"{row.Field}: {row.Value}"))),
            _ => throw new NotSupportedException($"Report format {job.Format} is not supported.")
        };
    }

    private static List<(string Field, string Value)> Rows(
        ReportJob job, ReportDefinition definition, DateTimeOffset generatedAt) =>
    [
        ("reportJobId", job.Id.ToString()),
        ("definitionCode", definition.Code),
        ("definitionVersion", definition.Version),
        ("periodFrom", job.From.ToString("O", CultureInfo.InvariantCulture)),
        ("periodTo", job.To.ToString("O", CultureInfo.InvariantCulture)),
        ("siteId", job.SiteId ?? "all"),
        ("fleetId", job.FleetId ?? "all"),
        ("dataSources", string.Join(" ", definition.DataSources)),
        ("requestedBy", job.RequestedBy.ToString()),
        ("generatedAt", generatedAt.ToString("O", CultureInfo.InvariantCulture))
    ];

    private static byte[] Csv(IEnumerable<(string Field, string Value)> rows)
    {
        var builder = new StringBuilder("field,value\n");
        foreach (var (field, value) in rows)
            builder.Append(field).Append(',').Append(Quote(value)).Append('\n');
        return Encoding.UTF8.GetBytes(builder.ToString());
    }

    private static string Quote(string value) =>
        value.IndexOfAny([',', '"', '\n', '\r']) < 0 ? value : $"\"{value.Replace("\"", "\"\"")}\"";

    /// <summary>Single-page PDF 1.4 (A4, Helvetica) without native dependencies.</summary>
    private static class MinimalPdf
    {
        public static byte[] Render(string title, IEnumerable<string> lines)
        {
            var content = new StringBuilder();
            var y = 790;
            content.Append(CultureInfo.InvariantCulture, $"BT /F1 16 Tf 50 {y} Td ({Escape(title)}) Tj ET\n");
            foreach (var line in lines)
            {
                y -= 18;
                content.Append(CultureInfo.InvariantCulture, $"BT /F1 10 Tf 50 {y} Td ({Escape(line)}) Tj ET\n");
            }

            var stream = content.ToString();
            string[] objects =
            [
                "<< /Type /Catalog /Pages 2 0 R >>",
                "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
                "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] " +
                "/Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>",
                "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
                $"<< /Length {Encoding.ASCII.GetByteCount(stream)} >>\nstream\n{stream}endstream"
            ];

            var pdf = new StringBuilder("%PDF-1.4\n");
            var offsets = new List<int>();
            for (var index = 0; index < objects.Length; index++)
            {
                offsets.Add(Encoding.ASCII.GetByteCount(pdf.ToString()));
                pdf.Append(CultureInfo.InvariantCulture, $"{index + 1} 0 obj\n{objects[index]}\nendobj\n");
            }

            var xref = Encoding.ASCII.GetByteCount(pdf.ToString());
            pdf.Append(CultureInfo.InvariantCulture, $"xref\n0 {objects.Length + 1}\n0000000000 65535 f \n");
            foreach (var offset in offsets)
                pdf.Append(offset.ToString("D10", CultureInfo.InvariantCulture)).Append(" 00000 n \n");
            pdf.Append(CultureInfo.InvariantCulture,
                $"trailer\n<< /Size {objects.Length + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF\n");
            return Encoding.ASCII.GetBytes(pdf.ToString());
        }

        /// <summary>Escapes PDF string delimiters and keeps the text in printable ASCII.</summary>
        private static string Escape(string text)
        {
            var builder = new StringBuilder(text.Length);
            foreach (var character in text)
            {
                builder.Append(character switch
                {
                    '\\' => "\\\\",
                    '(' => "\\(",
                    ')' => "\\)",
                    < ' ' or > '~' => "?",
                    _ => character.ToString()
                });
            }
            return builder.ToString();
        }
    }
}
