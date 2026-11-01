using MineSenseSafety.Shared.Infrastructure.Persistence.MongoDb;
using MongoDB.Driver;
using PrivacyAuditService.Domain.Model.Aggregates;
using PrivacyAuditService.Domain.Model.ReadModels;
using PrivacyAuditService.Domain.Repositories;

namespace PrivacyAuditService.Infrastructure.Persistence.MongoDb;

public class ProcessingConsentDocument : MongoAggregateDocument
{
    public string OperatorId { get; set; } = string.Empty;
    public string PolicyVersion { get; set; } = string.Empty;
    public DateTime DecidedAt { get; set; }
}

public class AccessAuditEntryDocument : MongoAggregateDocument
{
    public string ResourceType { get; set; } = string.Empty;
    public string ResourceId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public DateTime AccessedAt { get; set; }
    public string PrevHash { get; set; } = string.Empty;
    public string Hash { get; set; } = string.Empty;
}

public class ComplianceReportDocument : MongoAggregateDocument
{
    public string Status { get; set; } = string.Empty;
    public DateTime RequestedAt { get; set; }
}

/// <summary>Id = step key (the id of the event that produced the step).</summary>
public class IncidentLifecycleStepDocument : MongoAggregateDocument
{
    public string IncidentId { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; }
}

/// <summary>Consent decisions in MongoDB (ADR-005), newest first per operator.</summary>
public class MongoProcessingConsentRepository(IMongoDatabase database)
    : MongoRepository<ProcessingConsent, ProcessingConsentDocument>(database, "processing_consents"),
      IProcessingConsentRepository
{
    protected override ProcessingConsentDocument CreateDocument(ProcessingConsent consent) => new()
    {
        OperatorId = consent.OperatorId.ToString(),
        PolicyVersion = consent.PolicyVersion,
        DecidedAt = Utc(consent.DecidedAt)
    };

    protected override IEnumerable<CreateIndexModel<ProcessingConsentDocument>> Indexes() =>
        [new(Keys.Ascending(item => item.OperatorId).Descending(item => item.DecidedAt))];

    public async Task<IEnumerable<ProcessingConsent>> FindByOperatorIdAsync(Guid operatorId)
    {
        var consents = await ToEntitiesAsync(Collection.Find(item => item.OperatorId == operatorId.ToString()));
        return consents.OrderByDescending(consent => consent.DecidedAt).ToList();
    }
}

/// <summary>
/// Append-only, hash-chained access log in MongoDB (ADR-005). Unique indexes on the hash and on the
/// previous hash keep the chain linear: two writers that sealed the same previous entry cannot both
/// be stored (the second gets a conflict), so the log never forks. Entries are never replaced.
/// </summary>
public class MongoAccessAuditRepository(IMongoDatabase database)
    : MongoRepository<AccessAuditEntry, AccessAuditEntryDocument>(database, "access_audit_log"), IAccessAuditRepository
{
    protected override AccessAuditEntryDocument CreateDocument(AccessAuditEntry entry) => new()
    {
        ResourceType = entry.ResourceType,
        ResourceId = entry.ResourceId.ToString(),
        UserId = entry.UserId.ToString(),
        AccessedAt = Utc(entry.AccessedAt),
        PrevHash = entry.PrevHash,
        Hash = entry.Hash
    };

    protected override IEnumerable<CreateIndexModel<AccessAuditEntryDocument>> Indexes() =>
    [
        new(Keys.Ascending(item => item.Hash), new CreateIndexOptions { Unique = true }),
        new(Keys.Ascending(item => item.PrevHash), new CreateIndexOptions { Unique = true }),
        new(Keys.Ascending(item => item.ResourceType).Ascending(item => item.ResourceId).Ascending(item => item.AccessedAt)),
        new(Keys.Descending(item => item.AccessedAt))
    ];

    public override Task UpdateAsync(AccessAuditEntry aggregate) =>
        throw new InvalidOperationException("The access audit log is append-only: entries cannot be updated.");

    public async Task<IEnumerable<AccessAuditEntry>> FindByResourceAsync(string resourceType, Guid resourceId)
    {
        var entries = await ToEntitiesAsync(Collection.Find(item =>
            item.ResourceType == resourceType && item.ResourceId == resourceId.ToString()));
        return entries.OrderBy(entry => entry.AccessedAt).ToList();
    }
}

/// <summary>Compliance report requests in MongoDB (ADR-005).</summary>
public class MongoComplianceReportRepository(IMongoDatabase database)
    : MongoRepository<ComplianceReport, ComplianceReportDocument>(database, "compliance_reports"),
      IComplianceReportRepository
{
    protected override ComplianceReportDocument CreateDocument(ComplianceReport report) => new()
    {
        Status = report.Status.ToString(),
        RequestedAt = Utc(report.RequestedAt)
    };

    protected override IEnumerable<CreateIndexModel<ComplianceReportDocument>> Indexes() =>
        [new(Keys.Descending(item => item.RequestedAt))];
}

/// <summary>
/// Incident lifecycle steps (IncidentOpened / IncidentClosed) in MongoDB (ADR-005). The step key is the
/// document id, so a redelivered event appends nothing, as in memory.
/// </summary>
public class MongoIncidentLifecycleRepository(IMongoDatabase database) : IIncidentLifecycleRepository, IMongoCollectionInitializer
{
    private readonly IMongoCollection<IncidentLifecycleStepDocument> _collection =
        database.GetCollection<IncidentLifecycleStepDocument>("incident_lifecycle");

    public Task InitializeAsync(CancellationToken cancellationToken) => _collection.Indexes.CreateOneAsync(
        new CreateIndexModel<IncidentLifecycleStepDocument>(Builders<IncidentLifecycleStepDocument>.IndexKeys
            .Ascending(item => item.IncidentId).Ascending(item => item.OccurredAt)),
        cancellationToken: cancellationToken);

    public async Task AppendAsync(Guid incidentId, Guid stepKey, IncidentHistoryEntry entry)
    {
        try
        {
            await _collection.InsertOneAsync(new IncidentLifecycleStepDocument
            {
                Id = stepKey.ToString(), IncidentId = incidentId.ToString(),
                OccurredAt = entry.OccurredAt.UtcDateTime, Payload = AggregateSnapshot.Serialize(entry)
            });
        }
        catch (MongoWriteException exception) when (exception.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            // Step already appended by a previous delivery of the same event.
        }
    }

    public async Task<IEnumerable<IncidentHistoryEntry>> FindByIncidentIdAsync(Guid incidentId)
    {
        var documents = await _collection.Find(item => item.IncidentId == incidentId.ToString())
            .SortBy(item => item.OccurredAt).ToListAsync();
        return documents.Select(item => AggregateSnapshot.Deserialize<IncidentHistoryEntry>(item.Payload))
            .OrderBy(entry => entry.OccurredAt).ToList();
    }
}
