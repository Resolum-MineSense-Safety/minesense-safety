using FleetMonitoringService.Domain.Model.Aggregates;
using FleetMonitoringService.Domain.Model.ValueObjects;
using FleetMonitoringService.Domain.Repositories;
using MineSenseSafety.Shared.Infrastructure.Persistence.MongoDb;
using MongoDB.Driver;

namespace FleetMonitoringService.Infrastructure.Persistence.MongoDb;

/// <summary>Fleet, location and name are kept upper-cased: the criteria of the panel ignore case.</summary>
public class MonitoredOperatorDocument : MongoAggregateDocument
{
    public string OperatorId { get; set; } = string.Empty;
    public string Shift { get; set; } = string.Empty;
    public string FleetKey { get; set; } = string.Empty;
    public string LocationKey { get; set; } = string.Empty;
    public string FullNameKey { get; set; } = string.Empty;
    public string RiskLevel { get; set; } = string.Empty;

    /// <summary>Numeric risk level, so the panel can sort the highest risk first.</summary>
    public int RiskRank { get; set; }

    public string? MiningUnitId { get; set; }
    public string? FleetId { get; set; }
}

/// <summary>
/// Supervision panel of the fleet in MongoDB (ADR-005). One monitored operator per operator:
/// a unique index rejects a second registration that raced past the existence check.
/// </summary>
public class MongoMonitoredOperatorRepository(IMongoDatabase database)
    : MongoRepository<MonitoredOperator, MonitoredOperatorDocument>(database, "monitored_operators"),
      IMonitoredOperatorRepository
{
    protected override MonitoredOperatorDocument CreateDocument(MonitoredOperator monitored) => new()
    {
        OperatorId = monitored.OperatorId.ToString(),
        Shift = monitored.Shift.ToString(),
        FleetKey = Key(monitored.Fleet)!,
        LocationKey = Key(monitored.Location)!,
        FullNameKey = Key(monitored.FullName)!,
        RiskLevel = monitored.CurrentRiskLevel.ToString(),
        RiskRank = (int)monitored.CurrentRiskLevel,
        MiningUnitId = monitored.MiningUnitId?.ToString(),
        FleetId = monitored.FleetId?.ToString()
    };

    protected override IEnumerable<CreateIndexModel<MonitoredOperatorDocument>> Indexes() =>
    [
        new(Keys.Ascending(item => item.OperatorId), new CreateIndexOptions { Unique = true }),
        new(Keys.Descending(item => item.RiskRank).Ascending(item => item.FullNameKey)),
        new(Keys.Ascending(item => item.FleetId).Descending(item => item.RiskRank)),
        new(Keys.Ascending(item => item.MiningUnitId).Descending(item => item.RiskRank))
    ];

    public async Task<MonitoredOperator?> FindByOperatorIdAsync(Guid operatorId)
    {
        var document = await Collection.Find(item => item.OperatorId == operatorId.ToString()).FirstOrDefaultAsync();
        return document is null ? null : ToEntity(document);
    }

    public async Task<IEnumerable<MonitoredOperator>> FindByCriteriaAsync(
        Shift? shift, string? fleet, string? location, RiskLevel? riskLevel,
        Guid? miningUnitId, Guid? fleetId)
    {
        var filter = Filter.Empty;
        if (shift is not null)
            filter &= Filter.Eq(item => item.Shift, shift.Value.ToString());
        if (!string.IsNullOrWhiteSpace(fleet))
            filter &= Filter.Eq(item => item.FleetKey, Key(fleet));
        if (!string.IsNullOrWhiteSpace(location))
            filter &= Filter.Eq(item => item.LocationKey, Key(location));
        if (riskLevel is not null)
            filter &= Filter.Eq(item => item.RiskRank, (int)riskLevel.Value);
        if (miningUnitId is not null)
            filter &= Filter.Eq(item => item.MiningUnitId, miningUnitId.Value.ToString());
        if (fleetId is not null)
            filter &= Filter.Eq(item => item.FleetId, fleetId.Value.ToString());

        return await ToEntitiesAsync(Collection.Find(filter)
            .SortByDescending(item => item.RiskRank).ThenBy(item => item.FullNameKey).ThenBy(item => item.Id));
    }
}
