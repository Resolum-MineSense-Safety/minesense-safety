using MineSenseSafety.Shared.Infrastructure.Persistence.MongoDb;
using MongoDB.Driver;
using ReportingService.Domain.Model.Aggregates;
using ReportingService.Domain.Repositories;
using ReportingService.Domain.Services;

namespace ReportingService.Infrastructure.Persistence.MongoDb;

/// <summary>Code and version are kept upper-cased: indicators are looked up ignoring case.</summary>
public class ReportDefinitionDocument : MongoAggregateDocument
{
    public string CodeKey { get; set; } = string.Empty;
    public string VersionKey { get; set; } = string.Empty;
}

public class ReportJobDocument : MongoAggregateDocument
{
    public string Status { get; set; } = string.Empty;
    public DateTime RequestedAt { get; set; }
}

/// <summary>The rendered bytes travel in the payload until object storage (S3, ADR-008) is wired in.</summary>
public class ReportArtifactDocument : MongoAggregateDocument
{
    public string ReportJobId { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}

/// <summary>
/// Versioned indicators in MongoDB (ADR-005). Code + version is unique, as the define command expects;
/// the latest version is chosen with <see cref="ReportDefinitionVersionComparer"/>, as in memory.
/// </summary>
public class MongoReportDefinitionRepository(IMongoDatabase database)
    : MongoRepository<ReportDefinition, ReportDefinitionDocument>(database, "report_definitions"),
      IReportDefinitionRepository
{
    protected override ReportDefinitionDocument CreateDocument(ReportDefinition definition) => new()
    {
        CodeKey = Key(definition.Code)!,
        VersionKey = Key(definition.Version)!
    };

    protected override IEnumerable<CreateIndexModel<ReportDefinitionDocument>> Indexes() =>
        [new(Keys.Ascending(item => item.CodeKey).Ascending(item => item.VersionKey), new CreateIndexOptions { Unique = true })];

    public async Task<ReportDefinition?> FindByCodeAsync(string code)
    {
        var key = Key(code);
        var versions = await ToEntitiesAsync(Collection.Find(item => item.CodeKey == key));
        return versions.MaxBy(definition => definition.Version, ReportDefinitionVersionComparer.Instance);
    }

    public async Task<ReportDefinition?> FindByCodeAndVersionAsync(string code, string version)
    {
        var codeKey = Key(code);
        var versionKey = Key(version);
        var document = await Collection.Find(item => item.CodeKey == codeKey && item.VersionKey == versionKey)
            .FirstOrDefaultAsync();
        return document is null ? null : ToEntity(document);
    }
}

/// <summary>Report jobs in MongoDB (ADR-005); listed newest first by the query service.</summary>
public class MongoReportJobRepository(IMongoDatabase database)
    : MongoRepository<ReportJob, ReportJobDocument>(database, "report_jobs"), IReportJobRepository
{
    protected override ReportJobDocument CreateDocument(ReportJob job) => new()
    {
        Status = job.Status.ToString(),
        RequestedAt = Utc(job.RequestedAt)
    };

    protected override IEnumerable<CreateIndexModel<ReportJobDocument>> Indexes() =>
        [new(Keys.Descending(item => item.RequestedAt))];
}

/// <summary>Downloadable artifacts in MongoDB (ADR-005), one per report job.</summary>
public class MongoReportArtifactRepository(IMongoDatabase database)
    : MongoRepository<ReportArtifact, ReportArtifactDocument>(database, "report_artifacts"), IReportArtifactRepository
{
    protected override ReportArtifactDocument CreateDocument(ReportArtifact artifact) => new()
    {
        ReportJobId = artifact.ReportJobId.ToString(),
        ExpiresAt = Utc(artifact.ExpiresAt)
    };

    protected override IEnumerable<CreateIndexModel<ReportArtifactDocument>> Indexes() =>
        [new(Keys.Ascending(item => item.ReportJobId), new CreateIndexOptions { Unique = true })];

    public async Task<ReportArtifact?> FindByReportJobIdAsync(Guid reportJobId)
    {
        var document = await Collection.Find(item => item.ReportJobId == reportJobId.ToString()).FirstOrDefaultAsync();
        return document is null ? null : ToEntity(document);
    }
}
