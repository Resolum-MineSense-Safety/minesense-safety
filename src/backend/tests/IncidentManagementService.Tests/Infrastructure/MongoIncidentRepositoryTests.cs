using IncidentManagementService.Domain.Model.Aggregates;
using IncidentManagementService.Domain.Model.Commands;
using IncidentManagementService.Domain.Model.ValueObjects;
using IncidentManagementService.Infrastructure.Persistence.MongoDb;
using MineSenseSafety.Shared.Domain.Model;
using MineSenseSafety.Shared.Infrastructure.Persistence.MongoDb;
using MineSenseSafety.Tests.Support;

namespace IncidentManagementService.Tests.Infrastructure;

public class MongoIncidentRepositoryTests
{
    private static readonly DateTimeOffset Start = new(2026, 10, 15, 8, 0, 0, TimeSpan.Zero);

    private static Incident Opened(Guid operatorId, Guid alertId, DateTimeOffset openedAt) =>
        new(new OpenIncidentCommand(operatorId, alertId, IncidentSeverity.Critical), openedAt);

    private static Incident Closed()
    {
        var incident = Opened(Guid.NewGuid(), Guid.NewGuid(), Start);
        var supervisor = Guid.NewGuid();
        incident.Assign(supervisor, Start.AddMinutes(1));
        incident.RecordAction(IncidentActionType.PreventivePause, "Pause", "Operator rested", supervisor, Start.AddMinutes(2));
        incident.Close("Operator relieved", Start.AddMinutes(3));
        return incident;
    }

    [Fact]
    public void Snapshot_RoundTrip_KeepsActionsHistoryAndAttentionTime()
    {
        // Arrange
        var incident = Closed();

        // Act
        var restored = AggregateSnapshot.Deserialize<Incident>(AggregateSnapshot.Serialize(incident));

        // Assert
        Assert.Equal(IncidentStatus.Closed, restored.Status);
        Assert.Equal(incident.Actions.Single().Id, restored.Actions.Single().Id);
        Assert.Equal(incident.History, restored.History);
        Assert.Equal(incident.AttentionTime, restored.AttentionTime);
        Assert.Equal(incident.AssigneeId, restored.AssigneeId);
    }

    [MongoFact]
    public async Task Mongo_RealServer_FiltersAndKeepsOneIncidentPerAlert()
    {
        // Arrange
        await using var mongo = new MongoTestDatabase("incidents_tests");
        var repository = new MongoIncidentRepository(mongo.Database);
        await repository.InitializeAsync(CancellationToken.None);
        var operatorId = Guid.NewGuid();
        var newer = Opened(operatorId, Guid.NewGuid(), Start.AddMinutes(5));
        var older = Opened(operatorId, Guid.NewGuid(), Start.AddMinutes(1));
        var other = Closed();

        // Act
        await repository.AddAsync(newer);
        await repository.AddAsync(older);
        await repository.AddAsync(other);
        older.Assign(Guid.NewGuid(), Start.AddMinutes(6));
        await repository.UpdateAsync(older);
        await Assert.ThrowsAsync<DomainConflictException>(() =>
            repository.AddAsync(Opened(Guid.NewGuid(), newer.AlertId, Start.AddMinutes(9))));
        var restarted = new MongoIncidentRepository(mongo.Reopen());

        // Assert
        Assert.Equal(new[] { other.Id, older.Id, newer.Id }, (await restarted.FindAsync(null, null)).Select(item => item.Id));
        Assert.Equal(new[] { newer.Id }, (await restarted.FindAsync(IncidentStatus.Pending, operatorId)).Select(item => item.Id));
        Assert.Equal(new[] { older.Id }, (await restarted.FindAsync(IncidentStatus.Assigned, null)).Select(item => item.Id));
        Assert.Equal(newer.Id, (await restarted.FindByAlertIdAsync(newer.AlertId))!.Id);
        Assert.Single((await restarted.FindByIdAsync(other.Id))!.Actions);
        Assert.Null(await restarted.FindByAlertIdAsync(Guid.NewGuid()));
    }
}
