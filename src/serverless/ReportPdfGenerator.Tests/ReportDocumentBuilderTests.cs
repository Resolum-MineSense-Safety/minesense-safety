using System.Text;
using ReportPdfGenerator.Model;
using ReportPdfGenerator.Pdf;

namespace ReportPdfGenerator.Tests;

public class ReportDocumentBuilderTests
{
    private static readonly DateTimeOffset GeneratedAt = new(2026, 11, 2, 9, 30, 0, TimeSpan.Zero);

    private static string Render(ReportRequest request) =>
        Encoding.Latin1.GetString(ReportDocumentBuilder.Build(request, GeneratedAt));

    [Fact]
    public void Build_IncludesHeaderSummarySectionsAndDisclaimer()
    {
        // Act
        var pdf = Render(ReportRequests.Valid());

        // Assert
        Assert.Contains("(Reporte de gesti", pdf);
        Assert.Contains("Periodo: 01/10/2026 - 01/11/2026", pdf);
        Assert.Contains("(Resumen del periodo) Tj", pdf);
        Assert.Contains("(132) Tj", pdf);
        Assert.Contains("(Alertas por turno) Tj", pdf);
        Assert.Contains("(Turno 1) Tj", pdf);
        Assert.Contains("accidentes evitados.) Tj", pdf);
    }

    [Fact]
    public void Build_WithoutAttentionTimeAndEmptyTable_ShowsPlaceholders()
    {
        // Arrange
        var request = ReportRequests.Valid(rows: 0) with
        {
            Summary = new ReportSummary(0, 0, 0, 0, null)
        };

        // Act
        var pdf = Render(request);

        // Assert
        Assert.Contains("(Sin datos) Tj", pdf);
        Assert.Contains("(Sin registros en el periodo.) Tj", pdf);
    }
}
