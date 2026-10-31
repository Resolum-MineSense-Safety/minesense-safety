using AnalyticsService.Domain.Model.Aggregates;
using AnalyticsService.Domain.Model.ValueObjects;
using AnalyticsService.Domain.Repositories;
using MineSenseSafety.Shared.Infrastructure.Persistence.MongoDb;
using MongoDB.Driver;

namespace AnalyticsService.Infrastructure.Persistence.MongoDb;

/// <summary>The context dimensions are kept upper-cased: the analysis filters ignore case.</summary>
public class FatigueMetricDocument : MongoAggregateDocument
{
    public string OperatorId { get; set; } = string.Empty;
    public string AlertId { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; }
    public string ShiftKey { get; set; } = string.Empty;
    public string MineSiteKey { get; set; } = string.Empty;
    public string LocationKey { get; set; } = string.Empty;
    public string FleetKey { get; set; } = string.Empty;
}

/// <summary>
/// Historical fatigue metrics in MongoDB (ADR-005). A unique index on the alert keeps one metric per
/// alert when the AlertIssued event is redelivered. MongoDB stores milliseconds, so the period is
/// matched loosely in the query and exactly (half-open, as in memory) on the restored metrics.
/// </summary>
public class MongoFatigueMetricRepository(IMongoDatabase database)
    : MongoRepository<FatigueMetric, FatigueMetricDocument>(database, "fatigue_metrics"), IFatigueMetricRepository
{
    protected override FatigueMetricDocument CreateDocument(FatigueMetric metric) => new()
    {
        OperatorId = metric.OperatorId.ToString(),
        AlertId = metric.AlertId.ToString(),
        Severity = metric.Severity.ToString(),
        OccurredAt = Utc(metric.OccurredAt),
        ShiftKey = Key(metric.Context.Shift)!,
        MineSiteKey = Key(metric.Context.MineSite)!,
        LocationKey = Key(metric.Context.Location)!,
        FleetKey = Key(metric.Context.Fleet)!
    };

    protected override IEnumerable<CreateIndexModel<FatigueMetricDocument>> Indexes() =>
    [
        new(Keys.Ascending(item => item.AlertId), new CreateIndexOptions { Unique = true }),
        new(Keys.Ascending(item => item.OccurredAt)),
        new(Keys.Ascending(item => item.MineSiteKey).Ascending(item => item.OccurredAt)),
        new(Keys.Ascending(item => item.FleetKey).Ascending(item => item.OccurredAt))
    ];

    public async Task<IEnumerable<FatigueMetric>> FindAsync(AnalysisPeriod period, ContextFilter filter)
    {
        var query = Filter.Gte(item => item.OccurredAt, Utc(period.From)) & Filter.Lte(item => item.OccurredAt, Utc(period.To));
        if (!string.IsNullOrWhiteSpace(filter.Shift))
            query &= Filter.Eq(item => item.ShiftKey, Key(filter.Shift));
        if (!string.IsNullOrWhiteSpace(filter.MineSite))
            query &= Filter.Eq(item => item.MineSiteKey, Key(filter.MineSite));
        if (!string.IsNullOrWhiteSpace(filter.Location))
            query &= Filter.Eq(item => item.LocationKey, Key(filter.Location));
        if (!string.IsNullOrWhiteSpace(filter.Fleet))
            query &= Filter.Eq(item => item.FleetKey, Key(filter.Fleet));

        var metrics = await ToEntitiesAsync(Collection.Find(query).SortBy(item => item.OccurredAt).ThenBy(item => item.Id));
        return metrics.Where(metric => period.Contains(metric.OccurredAt) && filter.Matches(metric.Context))
            .OrderBy(metric => metric.OccurredAt).ToList();
    }

    public async Task<FatigueMetric?> FindByAlertIdAsync(Guid alertId)
    {
        var document = await Collection.Find(item => item.AlertId == alertId.ToString()).FirstOrDefaultAsync();
        return document is null ? null : ToEntity(document);
    }
}
