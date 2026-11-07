using ReportPdfGenerator.Model;

namespace ReportPdfGenerator.Tests;

internal static class ReportRequests
{
    public static ReportRequest Valid(int rows = 2) => new(
        Guid.NewGuid(),
        "Reporte de gestión de fatiga",
        new DateTimeOffset(2026, 10, 1, 0, 0, 0, TimeSpan.Zero),
        new DateTimeOffset(2026, 11, 1, 0, 0, 0, TimeSpan.Zero),
        "Gerencia de Seguridad",
        "Mina Antamina (Perú)",
        new ReportSummary(184, 132, 21, 20, 6.4),
        [
            new ReportSection("Alertas por turno", "Frecuencia de alertas por turno.",
                ["Turno", "Alertas"], Enumerable.Range(0, rows).Select(i => (IReadOnlyList<string>)[$"Turno {i}", $"{i}"]).ToList()),
            new ReportSection("Observaciones", "Sin incidentes abiertos al cierre.", null, null)
        ]);
}
