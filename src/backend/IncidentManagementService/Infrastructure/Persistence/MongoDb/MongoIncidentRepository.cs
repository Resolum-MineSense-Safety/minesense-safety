using IncidentManagementService.Domain.Model.Aggregates;
using IncidentManagementService.Domain.Model.ValueObjects;
using IncidentManagementService.Domain.Repositories;
using MineSenseSafety.Shared.Infrastructure.Persistence.MongoDb;
using MongoDB.Driver;

namespace IncidentManagementService.Infrastructure.Persistence.MongoDb;

public class IncidentDocument : MongoAggregateDocument
{
    public string OperatorId { get; set; } = string.Empty;
    public string AlertId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime OpenedAt { get; set; }
}

/// <summary>
/// Incidents in MongoDB (ADR-005), with their actions and history inside the document. A unique
/// index on the alert keeps "one incident per alert" when the AlertIssued event is redelivered.
/// </summary>
public class MongoIncidentRepository(IMongoDatabase database)
    : MongoRepository<Incident, IncidentDocument>(database, "incidents"), IIncidentRepository
{
    protected override IncidentDocument CreateDocument(Incident incident) => new()
    {
        OperatorId = incident.OperatorId.ToString(),
        AlertId = incident.AlertId.ToString(),
        Status = incident.Status.ToString(),
        OpenedAt = Utc(incident.OpenedAt)
    };

    protected override IEnumerable<CreateIndexModel<IncidentDocument>> Indexes() =>
    [
        new(Keys.Ascending(item => item.AlertId), new CreateIndexOptions { Unique = true }),
        new(Keys.Ascending(item => item.Status).Ascending(item => item.OpenedAt)),
        new(Keys.Ascending(item => item.OperatorId).Ascending(item => item.OpenedAt))
    ];

    public async Task<IEnumerable<Incident>> FindAsync(IncidentStatus? status, Guid? operatorId)
    {
        var filter = Filter.Empty;
        if (status is not null)
            filter &= Filter.Eq(item => item.Status, status.Value.ToString());
        if (operatorId is not null)
            filter &= Filter.Eq(item => item.OperatorId, operatorId.Value.ToString());
        return await ToEntitiesAsync(Collection.Find(filter).SortBy(item => item.OpenedAt).ThenBy(item => item.Id));
    }

    public async Task<Incident?> FindByAlertIdAsync(Guid alertId)
    {
        var document = await Collection.Find(item => item.AlertId == alertId.ToString()).FirstOrDefaultAsync();
        return document is null ? null : ToEntity(document);
    }
}
