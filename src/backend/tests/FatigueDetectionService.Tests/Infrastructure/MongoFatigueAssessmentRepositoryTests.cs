using FatigueDetectionService.Domain.Model.Aggregates;
using FatigueDetectionService.Domain.Model.Commands;
using FatigueDetectionService.Infrastructure.Persistence.MongoDb;
using MineSenseSafety.Shared.Infrastructure.Persistence.MongoDb;
using MineSenseSafety.Tests.Support;

namespace FatigueDetectionService.Tests.Infrastructure;

public class MongoFatigueAssessmentRepositoryTests
{
    private static readonly DateTimeOffset Start = new(2026, 10, 6, 8, 0, 0, TimeSpan.Zero);

    private static FatigueAssessment Assessment(Guid operatorId, Guid sessionId, int minutes, double perclos = 0.20) =>
        new(new AssessFatigueCommand(operatorId, sessionId, perclos, 18, 35), Start.AddMinutes(minutes));

    [Fact]
    public void Snapshot_RoundTrip_KeepsSignalsAndRiskLevel()
    {
        // Arrange
        var assessment = Assessment(Guid.NewGuid(), Guid.NewGuid(), 0, 0.45);

        // Act
        var restored = AggregateSnapshot.Deserialize<FatigueAssessment>(AggregateSnapshot.Serialize(assessment));

        // Assert
        Assert.Equal(assessment.Id, restored.Id);
        Assert.Equal(assessment.Signals, restored.Signals);
        Assert.Equal(assessment.RiskLevel, restored.RiskLevel);
        Assert.Equal(assessment.AssessedAt, restored.AssessedAt);
    }

    [MongoFact]
    public async Task Mongo_RealServer_OrdersByOperatorAndSession()
    {
        // Arrange
        await using var mongo = new MongoTestDatabase("fatigue_tests");
        var repository = new MongoFatigueAssessmentRepository(mongo.Database);
        await repository.InitializeAsync(CancellationToken.None);
        var operatorId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var first = Assessment(operatorId, sessionId, 0);
        var second = Assessment(operatorId, sessionId, 10);
        var otherSession = Assessment(operatorId, Guid.NewGuid(), 20);

        // Act
        await repository.AddAsync(second);
        await repository.AddAsync(otherSession);
        await repository.AddAsync(first);
        var restarted = new MongoFatigueAssessmentRepository(mongo.Reopen());

        // Assert
        Assert.Equal(new[] { otherSession.Id, second.Id, first.Id },
            (await restarted.FindByOperatorIdAsync(operatorId)).Select(item => item.Id));
        Assert.Equal(new[] { first.Id, second.Id },
            (await restarted.FindByMonitoringSessionIdAsync(sessionId)).Select(item => item.Id));
        Assert.Equal(first.Signals, (await restarted.FindByIdAsync(first.Id))!.Signals);
        Assert.Empty(await restarted.FindByOperatorIdAsync(Guid.NewGuid()));
    }
}
