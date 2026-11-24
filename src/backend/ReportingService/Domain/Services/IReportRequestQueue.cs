namespace ReportingService.Domain.Services;

/// <summary>Queue the ReportPdfGenerator Lambda consumes (ADR-008, section 3.5).</summary>
public interface IReportRequestQueue
{
    Task SendAsync(string messageBody, CancellationToken cancellationToken);
}
