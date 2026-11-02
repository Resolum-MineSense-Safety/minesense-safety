using MineSenseSafety.Shared.Domain.Model;
using MineSenseSafety.Shared.Infrastructure.Persistence.MongoDb;
using MineSenseSafety.Tests.Support;
using MonitoringService.Domain.Model.Aggregates;
using MonitoringService.Domain.Model.Commands;
using MonitoringService.Domain.Model.ValueObjects;
using MonitoringService.Infrastructure.Persistence.PostgreSql;

namespace MonitoringService.Tests.Infrastructure;

public class PostgresMonitoringSessionRepositoryTests
{
    private static readonly DateTimeOffset Start = new(2026, 10, 6, 7, 0, 0, 123, TimeSpan.Zero);

    private static readonly SignalType[] BothSignals = [SignalType.EyeTracking, SignalType.HeartRate];

    private static MonitoringSession Started(Guid operatorId, int minutes) =>
        new(new StartMonitoringCommand(operatorId, "TRK-20", BothSignals), Start.AddMinutes(minutes).AddTicks(5));

    [Fact]
    public void Snapshot_RoundTrip_RestoresSignalsAndTraceability()
    {
        // Arrange
        var session = Started(Guid.NewGuid(), 0);
        session.ReportSignalLost(SignalType.HeartRate, Start.AddMinutes(5));

        // Act
        var restored = AggregateSnapshot.Deserialize<MonitoringSession>(AggregateSnapshot.Serialize(session));

        // Assert
        Assert.Equal(MonitoringStatus.Partial, restored.Status);
        Assert.Equal(new[] { SignalType.EyeTracking }, restored.AvailableSignals);
        Assert.Equal(new[] { SignalType.HeartRate }, restored.MissingSignals);
        Assert.Equal(session.SignalEvents, restored.SignalEvents);
        Assert.Equal(session.StartedAt.Ticks, restored.StartedAt.Ticks);
    }

    [PostgresFact]
    public async Task Sessions_RealServer_PersistAcrossRestartAndFilterByOperatorAndStatus()
    {
        // Arrange
        await using var postgres = new PostgresTestDatabase("monitoring_tests");
        var repository = await postgres.PrepareAsync(new PostgresMonitoringSessionRepository(postgres.DataSource));
        var operatorId = Guid.NewGuid();
        var finished = Started(operatorId, 0);
        var partial = Started(Guid.NewGuid(), 10);

        // Act
        await repository.AddAsync(finished);
        await repository.AddAsync(partial);
        finished.Stop(Start.AddHours(8));
        await repository.UpdateAsync(finished);
        var current = Started(operatorId, 600);
        await repository.AddAsync(current);
        partial.ReportSignalLost(SignalType.EyeTracking, Start.AddMinutes(20));
        await repository.UpdateAsync(partial);
        var restarted = new PostgresMonitoringSessionRepository(postgres.Reopen());

        // Assert
        Assert.Equal(current.Id, (await restarted.FindOpenByOperatorIdAsync(operatorId))!.Id);
        Assert.Equal(current.Id, (await restarted.FindLatestByOperatorIdAsync(operatorId))!.Id);
        Assert.Null(await restarted.FindOpenByOperatorIdAsync(Guid.NewGuid()));
        Assert.Equal(new[] { finished.Id }, (await restarted.FindByStatusAsync(MonitoringStatus.Stopped)).Select(s => s.Id));
        Assert.Equal(new[] { partial.Id }, (await restarted.FindByStatusAsync(MonitoringStatus.Partial)).Select(s => s.Id));
        Assert.Equal(new[] { current.Id }, (await restarted.FindByStatusAsync(MonitoringStatus.Active)).Select(s => s.Id));
        Assert.Equal(new[] { current.Id, partial.Id, finished.Id },
            (await restarted.FindByStatusAsync(null)).Select(s => s.Id));
        var stored = (await restarted.FindByIdAsync(partial.Id))!;
        Assert.Equal(partial.SignalEvents, stored.SignalEvents);
    }

    [PostgresFact]
    public async Task Sessions_RealServer_AllowOnlyOneOpenSessionPerOperator()
    {
        // Arrange
        await using var postgres = new PostgresTestDatabase("monitoring_tests");
        var repository = await postgres.PrepareAsync(new PostgresMonitoringSessionRepository(postgres.DataSource));
        var operatorId = Guid.NewGuid();
        var open = Started(operatorId, 0);
        await repository.AddAsync(open);

        // Act
        var racing = await Assert.ThrowsAsync<DomainConflictException>(() => repository.AddAsync(Started(operatorId, 1)));
        open.Stop(Start.AddHours(1));
        await repository.UpdateAsync(open);
        var next = Started(operatorId, 120);
        await repository.AddAsync(next);

        // Assert
        Assert.NotNull(racing);
        Assert.Equal(next.Id, (await repository.FindOpenByOperatorIdAsync(operatorId))!.Id);
        Assert.Equal(2, (await repository.ListAsync()).Count());
    }
}
