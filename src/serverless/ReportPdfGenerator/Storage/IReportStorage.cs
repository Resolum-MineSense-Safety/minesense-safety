namespace ReportPdfGenerator.Storage;

public interface IReportStorage
{
    /// <summary>Stores the document and returns its key.</summary>
    Task<string> SaveAsync(Guid reportId, DateTimeOffset requestedAt, byte[] document, CancellationToken cancellationToken);
}
