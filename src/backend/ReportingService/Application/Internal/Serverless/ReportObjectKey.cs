using System.Globalization;

namespace ReportingService.Application.Internal.Serverless;

/// <summary>
/// Key of a report in the bucket, the same one the ReportPdfGenerator Lambda computes from the
/// <c>requestedAt</c> of the message: reports/yyyy/MM/{reportId}.pdf, in UTC.
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
