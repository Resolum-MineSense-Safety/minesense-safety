namespace ReportingService.Domain.Services;

/// <summary>Object storage where the ReportPdfGenerator Lambda leaves the PDFs (ADR-008, section 3.5).</summary>
public interface IReportObjectStore
{
    /// <summary>Opens the object for reading; null when it does not exist (yet).</summary>
    Task<Stream?> OpenReadAsync(string key, CancellationToken cancellationToken);
}
