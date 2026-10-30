using System.Security.Cryptography;
using MineSenseSafety.Shared.Domain.Model;

namespace ReportingService.Domain.Model.Aggregates;

/// <summary>
/// Generated report artifact (US22 export) with its SHA-256 checksum and expiration.
/// The bytes are kept with the artifact until object storage (S3, ADR-008) is wired in.
/// </summary>
public class ReportArtifact : AggregateRoot
{
    private readonly byte[] _content;

    public Guid ReportJobId { get; private set; }
    public string Uri { get; private set; } = string.Empty;
    public string FileName { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public string Checksum { get; private set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; private set; }
    public ReadOnlyMemory<byte> Content => _content;

    public ReportArtifact(Guid reportJobId, string uri, string fileName, string contentType,
        byte[] content, DateTimeOffset expiresAt)
    {
        if (reportJobId == Guid.Empty)
            throw new DomainException("A report artifact must belong to a report job.");
        if (string.IsNullOrWhiteSpace(uri))
            throw new DomainException("A report artifact must have a location.");
        if (content is null || content.Length == 0)
            throw new DomainException("A report artifact must have content.");

        ReportJobId = reportJobId;
        Uri = uri;
        FileName = fileName;
        ContentType = contentType;
        _content = content.ToArray();
        Checksum = $"sha256:{Convert.ToHexString(SHA256.HashData(_content)).ToLowerInvariant()}";
        ExpiresAt = expiresAt;
    }

    public bool IsExpired(DateTimeOffset now) => now >= ExpiresAt;
}
