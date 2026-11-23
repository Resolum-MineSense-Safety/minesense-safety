using AnalyticsService.Domain.Model.Aggregates;
using AnalyticsService.Domain.Repositories;
using MineSenseSafety.Shared.Infrastructure.Persistence.MongoDb;
using MongoDB.Driver;

namespace AnalyticsService.Infrastructure.Persistence.MongoDb;

public class PreventiveMeasureDocument : MongoAggregateDocument
{
    public string MeasureType { get; set; } = string.Empty;
    public DateTime AppliedAt { get; set; }
}

/// <summary>Preventive measures in MongoDB (ADR-005).</summary>
public class MongoPreventiveMeasureRepository(IMongoDatabase database)
    : MongoRepository<PreventiveMeasure, PreventiveMeasureDocument>(database, "preventive_measures"),
      IPreventiveMeasureRepository
{
    protected override PreventiveMeasureDocument CreateDocument(PreventiveMeasure measure) => new()
    {
        MeasureType = measure.MeasureType.ToString(),
        AppliedAt = Utc(measure.AppliedAt)
    };

    protected override IEnumerable<CreateIndexModel<PreventiveMeasureDocument>> Indexes() =>
        [new(Keys.Ascending(item => item.AppliedAt))];
}
