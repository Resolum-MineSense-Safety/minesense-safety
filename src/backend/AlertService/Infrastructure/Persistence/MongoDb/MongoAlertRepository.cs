using AlertService.Domain.Model.Aggregates;
using AlertService.Domain.Model.ValueObjects;
using AlertService.Domain.Repositories;
using MineSenseSafety.Shared.Infrastructure.Persistence.MongoDb;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace AlertService.Infrastructure.Persistence.MongoDb;

public class AlertDocument : MongoAggregateDocument
{
    public string OperatorId { get; set; } = string.Empty;

    /// <summary>Absent for alerts issued without an assessment, so the unique index ignores them.</summary>
    [BsonIgnoreIfNull]
    public string? AssessmentId { get; set; }

    public string Status { get; set; } = string.Empty;
    public DateTime IssuedAt { get; set; }
}

/// <summary>
/// Alerts in MongoDB (ADR-005). A unique sparse index on the assessment keeps the rule "one
/// assessment produces at most one alert" even when the FatigueAssessed event and a direct call race.
/// </summary>
public class MongoAlertRepository(IMongoDatabase database)
    : MongoRepository<Alert, AlertDocument>(database, "alerts"), IAlertRepository
{
    protected override AlertDocument CreateDocument(Alert alert) => new()
    {
        OperatorId = alert.OperatorId.ToString(),
        AssessmentId = alert.AssessmentId == Guid.Empty ? null : alert.AssessmentId.ToString(),
        Status = alert.Status.ToString(),
        IssuedAt = Utc(alert.IssuedAt)
    };

    protected override IEnumerable<CreateIndexModel<AlertDocument>> Indexes() =>
    [
        new(Keys.Ascending(item => item.AssessmentId), new CreateIndexOptions { Unique = true, Sparse = true }),
        new(Keys.Ascending(item => item.OperatorId).Descending(item => item.IssuedAt)),
        new(Keys.Ascending(item => item.Status).Ascending(item => item.IssuedAt))
    ];

    public async Task<IEnumerable<Alert>> FindByOperatorIdAsync(Guid operatorId) =>
        await ToEntitiesAsync(Collection.Find(item => item.OperatorId == operatorId.ToString())
            .SortByDescending(item => item.IssuedAt).ThenBy(item => item.Id));

    public async Task<Alert?> FindByAssessmentIdAsync(Guid assessmentId)
    {
        var document = await Collection.Find(item => item.AssessmentId == assessmentId.ToString()).FirstOrDefaultAsync();
        return document is null ? null : ToEntity(document);
    }

    public async Task<IEnumerable<Alert>> FindByStatusAsync(AlertStatus status)
    {
        var key = status.ToString();
        return await ToEntitiesAsync(Collection.Find(item => item.Status == key)
            .SortBy(item => item.IssuedAt).ThenBy(item => item.Id));
    }
}
