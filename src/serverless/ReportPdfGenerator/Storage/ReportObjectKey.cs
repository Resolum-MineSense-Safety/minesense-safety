using System.Globalization;

namespace ReportPdfGenerator.Storage;

/// <summary>
/// Key of a report in the bucket: reports/yyyy/MM/{reportId}.pdf, in UTC. Reporting Service computes the
/// same key to find the PDF; a contract test of ReportingService.Tests keeps both in step.
/// </summary>
public static class ReportObjectKey
{
    public static string For(Guid reportId, DateTimeOffset requestedAt)
    {
        var utc = requestedAt.ToUniversalTime();
        var year = utc.ToString("yyyy", CultureInfo.InvariantCulture);
        var month = utc.ToString("MM", CultureInfo.InvariantCulture);
        return $"reports/{year}/{month}/{reportId}.pdf";
    }
}
