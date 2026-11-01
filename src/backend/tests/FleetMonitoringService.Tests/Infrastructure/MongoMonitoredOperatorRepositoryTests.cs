using FleetMonitoringService.Domain.Model.Aggregates;
using FleetMonitoringService.Domain.Model.Commands;
using FleetMonitoringService.Domain.Model.ValueObjects;
using FleetMonitoringService.Infrastructure.Persistence.MongoDb;
using MineSenseSafety.Shared.Domain.Model;
using MineSenseSafety.Shared.Infrastructure.Persistence.MongoDb;
using MineSenseSafety.Tests.Support;

namespace FleetMonitoringService.Tests.Infrastructure;

public class MongoMonitoredOperatorRepositoryTests
{
    private static readonly DateTimeOffset Start = new(2026, 10, 6, 8, 0, 0, TimeSpan.Zero);
    private static readonly Guid MiningUnit = Guid.NewGuid();
    private static readonly Guid FleetId = Guid.NewGuid();

    private static MonitoredOperator Operator(string name, string fleet, Shift shift, Guid? operatorId = null) =>
        new(new RegisterMonitoredOperatorCommand(operatorId ?? Guid.NewGuid(), name, "CAT-793", fleet, shift,
            "Pit North", MiningUnit, FleetId), Start);

    [Fact]
    public void Snapshot_RoundTrip_KeepsScopeAndRisk()
    {
        // Arrange
        var monitored = Operator("Ana Quispe", "Haul A", Shift.Night);
        monitored.UpdateRiskLevel(RiskLevel.Critical, Start.AddMinutes(1));

        // Act
        var restored = AggregateSnapshot.Deserialize<MonitoredOperator>(AggregateSnapshot.Serialize(monitored));

        // Assert
        Assert.Equal(RiskLevel.Critical, restored.CurrentRiskLevel);
        Assert.Equal(monitored.MiningUnitId, restored.MiningUnitId);
        Assert.Equal(monitored.LastUpdatedAt, restored.LastUpdatedAt);
        Assert.Equal("Ana Quispe", restored.FullName);
    }

    [MongoFact]
    public async Task Mongo_RealServer_FiltersIgnoringCaseAndSortsByRiskThenName()
    {
        // Arrange
        await using var mongo = new MongoTestDatabase("fleet_tests");
        var repository = new MongoMonitoredOperatorRepository(mongo.Database);
        await repository.InitializeAsync(CancellationToken.None);
        var bruno = Operator("bruno Diaz", "Haul A", Shift.Day);
        var ana = Operator("Ana Quispe", "Haul A", Shift.Day);
        var carla = Operator("Carla Rojas", "haul a", Shift.Day);
        var night = Operator("Dario Paz", "Haul B", Shift.Night);

        // Act
        foreach (var monitored in new[] { bruno, ana, carla, night })
            await repository.AddAsync(monitored);
        carla.UpdateRiskLevel(RiskLevel.Critical, Start.AddMinutes(2));
        await repository.UpdateAsync(carla);
        await Assert.ThrowsAsync<DomainConflictException>(() =>
            repository.AddAsync(Operator("Ana Again", "Haul A", Shift.Day, ana.OperatorId)));
        var restarted = new MongoMonitoredOperatorRepository(mongo.Reopen());

        // Assert
        Assert.Equal(new[] { carla.Id, ana.Id, bruno.Id },
            (await restarted.FindByCriteriaAsync(Shift.Day, " HAUL A ", "pit north", null, MiningUnit, FleetId))
            .Select(item => item.Id));
        Assert.Equal(new[] { carla.Id },
            (await restarted.FindByCriteriaAsync(null, null, null, RiskLevel.Critical, null, null)).Select(item => item.Id));
        Assert.Equal(4, (await restarted.FindByCriteriaAsync(null, null, null, null, null, null)).Count());
        Assert.Empty(await restarted.FindByCriteriaAsync(null, null, null, null, Guid.NewGuid(), null));
        Assert.Equal(night.Id, (await restarted.FindByOperatorIdAsync(night.OperatorId))!.Id);
    }
}
