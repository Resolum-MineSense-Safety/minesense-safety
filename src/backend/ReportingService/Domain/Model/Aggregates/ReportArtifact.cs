using MineSenseSafety.Shared.Domain.Model;

namespace ReportingService.Domain.Model.Aggregates;

/// <summary>
/// Generated report artifact metadata (US22 export).
/// The file itself lives in object storage; integrity is verified via checksum.
/// Scaffold: shape only, rendering arrives with Fase 2.
/// </summary>
public class ReportArtifact : AggregateRoot
{
    public Guid ReportJobId { get; private set; }
    public string Uri { get; private set; } = string.Empty;
    public string Checksum { get; private set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; private set; }

    public ReportArtifact(Guid reportJobId, string uri, string checksum, DateTimeOffset expiresAt)
    {
        if (reportJobId == Guid.Empty)
            throw new DomainException("A report artifact must belong to a report job.");
        if (string.IsNullOrWhiteSpace(uri))
            throw new DomainException("A report artifact must have a location.");

        ReportJobId = reportJobId;
        Uri = uri;
        Checksum = checksum;
        ExpiresAt = expiresAt;
    }
}
