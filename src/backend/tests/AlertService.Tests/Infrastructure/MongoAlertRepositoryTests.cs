using AlertService.Domain.Model.Aggregates;
using AlertService.Domain.Model.Commands;
using AlertService.Domain.Model.ValueObjects;
using AlertService.Infrastructure.Persistence.MongoDb;
using MineSenseSafety.Shared.Domain.Model;
using MineSenseSafety.Shared.Infrastructure.Persistence.MongoDb;
using MineSenseSafety.Tests.Support;

namespace AlertService.Tests.Infrastructure;

public class MongoAlertRepositoryTests
{
    private static readonly DateTimeOffset Start = new(2026, 10, 6, 8, 0, 0, 123, TimeSpan.Zero);

    private static Alert IssuedAlert(Guid operatorId, Guid assessmentId, int seconds) =>
        new(new IssueAlertCommand(operatorId, assessmentId, AlertSeverity.Critical), Start.AddSeconds(seconds).AddTicks(7));

    [Fact]
    public void Snapshot_RoundTrip_RestoresPrivateStateExactly()
    {
        // Arrange
        var alert = IssuedAlert(Guid.NewGuid(), Guid.NewGuid(), 0);
        alert.Acknowledge(Start.AddSeconds(5).ToOffset(TimeSpan.FromHours(-5)));

        // Act
        var restored = AggregateSnapshot.Deserialize<Alert>(AggregateSnapshot.Serialize(alert));

        // Assert
        Assert.Equal(alert.Id, restored.Id);
        Assert.Equal(AlertStatus.Acknowledged, restored.Status);
        Assert.Equal(alert.IssuedAt.Ticks, restored.IssuedAt.Ticks);
        Assert.Equal(alert.AcknowledgedAt!.Value.Offset, restored.AcknowledgedAt!.Value.Offset);
        Assert.Equal(AggregateSnapshot.Serialize(alert), AggregateSnapshot.Serialize(restored));
    }

    [MongoFact]
    public async Task Mongo_RealServer_PersistsQueriesAndKeepsOneAlertPerAssessment()
    {
        // Arrange
        await using var mongo = new MongoTestDatabase("alerts_tests");
        var repository = new MongoAlertRepository(mongo.Database);
        await repository.InitializeAsync(CancellationToken.None);
        var operatorId = Guid.NewGuid();
        var assessmentId = Guid.NewGuid();
        var older = IssuedAlert(operatorId, assessmentId, 10);
        var newer = IssuedAlert(operatorId, Guid.Empty, 50);
        var direct = IssuedAlert(operatorId, Guid.Empty, 70);

        // Act
        await repository.AddAsync(older);
        await repository.AddAsync(newer);
        await repository.AddAsync(direct);
        older.Acknowledge(Start.AddSeconds(20));
        await repository.UpdateAsync(older);
        var duplicate = await Assert.ThrowsAsync<DomainConflictException>(() =>
            repository.AddAsync(IssuedAlert(Guid.NewGuid(), assessmentId, 90)));
        var restarted = new MongoAlertRepository(mongo.Reopen());

        // Assert
        Assert.NotNull(duplicate);
        Assert.Equal(AlertStatus.Acknowledged, (await restarted.FindByIdAsync(older.Id))!.Status);
        Assert.Equal(older.Id, (await restarted.FindByAssessmentIdAsync(assessmentId))!.Id);
        Assert.Equal(new[] { direct.Id, newer.Id, older.Id },
            (await restarted.FindByOperatorIdAsync(operatorId)).Select(alert => alert.Id));
        Assert.Equal(new[] { newer.Id, direct.Id },
            (await restarted.FindByStatusAsync(AlertStatus.Issued)).Select(alert => alert.Id));
        Assert.Equal(3, (await restarted.ListAsync()).Count());
        Assert.Equal(older.IssuedAt, (await restarted.FindByIdAsync(older.Id))!.IssuedAt);
    }
}
