using MineSenseSafety.Shared.Domain.Model;
using MineSenseSafety.Shared.Infrastructure.Persistence.MongoDb;
using MineSenseSafety.Tests.Support;
using OperationsService.Domain.Model.Aggregates;
using OperationsService.Domain.Model.Commands;
using OperationsService.Domain.Model.ValueObjects;
using OperationsService.Infrastructure.Persistence.PostgreSql;

namespace OperationsService.Tests.Infrastructure;

public class PostgresOperationsRepositoriesTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 6, 8, 0, 0, 123, TimeSpan.Zero);

    [Fact]
    public void Snapshot_RoundTrip_RestoresLocationsAndAssignmentDates()
    {
        // Arrange
        var unit = new MiningUnit(new RegisterMiningUnitCommand(" ant-01 ", "Antamina", "Ancash"));
        unit.AddLocation("Pit North", LocationKind.Pit);
        unit.AddLocation("Crusher 2", LocationKind.Crusher);
        var assignment = new OperatorAssignment(new CreateAssignmentCommand(Guid.NewGuid(), Guid.NewGuid(), Shift.Night,
            new DateOnly(2026, 10, 1), new DateOnly(2026, 12, 31)), Now);
        assignment.End(new DateOnly(2026, 11, 15));

        // Act
        var restoredUnit = AggregateSnapshot.Deserialize<MiningUnit>(AggregateSnapshot.Serialize(unit));
        var restoredAssignment = AggregateSnapshot.Deserialize<OperatorAssignment>(AggregateSnapshot.Serialize(assignment));

        // Assert
        Assert.Equal("ANT-01", restoredUnit.BusinessCode);
        Assert.Equal(unit.Locations, restoredUnit.Locations);
        Assert.Equal(Shift.Night, restoredAssignment.Shift);
        Assert.Equal(assignment.ValidTo, restoredAssignment.ValidTo);
        Assert.Equal(AssignmentStatus.Ended, restoredAssignment.Status);
    }

    [PostgresFact]
    public async Task MiningUnitsAndFleets_RealServer_PersistAndKeepCodesAndNamesUnique()
    {
        // Arrange
        await using var postgres = new PostgresTestDatabase("operations_tests");
        var units = await postgres.PrepareAsync(new PostgresMiningUnitRepository(postgres.DataSource));
        var fleets = await postgres.PrepareAsync(new PostgresFleetRepository(postgres.DataSource));
        var antamina = new MiningUnit(new RegisterMiningUnitCommand("ANT-01", "Antamina", "Ancash"));
        var yanacocha = new MiningUnit(new RegisterMiningUnitCommand("YAN-01", "Yanacocha", "Cajamarca"));

        // Act
        await units.AddAsync(antamina);
        await units.AddAsync(yanacocha);
        antamina.AddLocation("Pit North", LocationKind.Pit);
        await units.UpdateAsync(antamina);
        var duplicateCode = await Assert.ThrowsAsync<DomainConflictException>(() =>
            units.AddAsync(new MiningUnit(new RegisterMiningUnitCommand(" ant-01", "Other", "Lima"))));
        await fleets.AddAsync(new Fleet(new RegisterFleetCommand(antamina.Id, "Trucks")));
        await fleets.AddAsync(new Fleet(new RegisterFleetCommand(antamina.Id, "Loaders")));
        await fleets.AddAsync(new Fleet(new RegisterFleetCommand(yanacocha.Id, "Trucks")));
        var duplicateName = await Assert.ThrowsAsync<DomainConflictException>(() =>
            fleets.AddAsync(new Fleet(new RegisterFleetCommand(antamina.Id, "TRUCKS"))));
        var restartedUnits = new PostgresMiningUnitRepository(postgres.Reopen());
        var restartedFleets = new PostgresFleetRepository(postgres.Reopen());

        // Assert
        Assert.NotNull(duplicateCode);
        Assert.NotNull(duplicateName);
        var stored = (await restartedUnits.FindByBusinessCodeAsync("ant-01"))!;
        Assert.Equal(antamina.Id, stored.Id);
        Assert.Equal(new[] { new Location("Pit North", LocationKind.Pit) }, stored.Locations);
        Assert.Null(await restartedUnits.FindByBusinessCodeAsync("XXX-99"));
        Assert.Equal(new[] { "Loaders", "Trucks" },
            (await restartedFleets.FindByMiningUnitIdAsync(antamina.Id)).Select(fleet => fleet.Name));
        Assert.Single(await restartedFleets.FindByMiningUnitIdAsync(yanacocha.Id));
    }

    [PostgresFact]
    public async Task VehiclesAndOperators_RealServer_FilterUpdateAndKeepCodesUnique()
    {
        // Arrange
        await using var postgres = new PostgresTestDatabase("operations_tests");
        var vehicles = await postgres.PrepareAsync(new PostgresVehicleRepository(postgres.DataSource));
        var operators = await postgres.PrepareAsync(new PostgresOperatorRepository(postgres.DataSource));
        var fleetA = Guid.NewGuid();
        var fleetB = Guid.NewGuid();
        var unitA = Guid.NewGuid();
        var truck = new Vehicle(new RegisterVehicleCommand("trk-20", "CAT 797F", fleetA));
        var loader = new Vehicle(new RegisterVehicleCommand("LDR-05", "CAT 994K", fleetA));
        var worker = new Operator(new RegisterOperatorCommand("op-002", "Luis", "Rojas", unitA));
        var other = new Operator(new RegisterOperatorCommand("OP-001", "Ana", "Torres", unitA));

        // Act
        await vehicles.AddAsync(truck);
        await vehicles.AddAsync(loader);
        truck.Update(new UpdateVehicleCommand(truck.Id, "CAT 797F", VehicleStatus.InMaintenance, fleetB));
        await vehicles.UpdateAsync(truck);
        var duplicateVehicle = await Assert.ThrowsAsync<DomainConflictException>(() =>
            vehicles.AddAsync(new Vehicle(new RegisterVehicleCommand("TRK-20 ", "Other", fleetB))));
        await operators.AddAsync(worker);
        await operators.AddAsync(other);
        var duplicateOperator = await Assert.ThrowsAsync<DomainConflictException>(() =>
            operators.AddAsync(new Operator(new RegisterOperatorCommand("Op-002", "Eva", "Diaz", unitA))));
        var restartedVehicles = new PostgresVehicleRepository(postgres.Reopen());
        var restartedOperators = new PostgresOperatorRepository(postgres.Reopen());

        // Assert
        Assert.NotNull(duplicateVehicle);
        Assert.NotNull(duplicateOperator);
        var storedTruck = (await restartedVehicles.FindByCodeAsync(" trk-20"))!;
        Assert.Equal(VehicleStatus.InMaintenance, storedTruck.Status);
        Assert.Equal(new[] { loader.Id }, (await restartedVehicles.FindByFleetIdAsync(fleetA)).Select(v => v.Id));
        Assert.Equal(new[] { truck.Id }, (await restartedVehicles.FindByFleetIdAsync(fleetB)).Select(v => v.Id));
        Assert.Equal(worker.Id, (await restartedOperators.FindByEmployeeCodeAsync("OP-002"))!.Id);
        Assert.Equal(new[] { "OP-001", "OP-002" },
            (await restartedOperators.FindByMiningUnitIdAsync(unitA)).Select(o => o.EmployeeCode));
        Assert.Empty(await restartedOperators.FindByMiningUnitIdAsync(Guid.NewGuid()));
    }

    [PostgresFact]
    public async Task OperatorAssignments_RealServer_KeepHistoryOrderedByValidityAcrossRestart()
    {
        // Arrange
        await using var postgres = new PostgresTestDatabase("operations_tests");
        var repository = await postgres.PrepareAsync(new PostgresOperatorAssignmentRepository(postgres.DataSource));
        var operatorId = Guid.NewGuid();
        var vehicleA = Guid.NewGuid();
        var vehicleB = Guid.NewGuid();
        var current = new OperatorAssignment(new CreateAssignmentCommand(operatorId, vehicleA, Shift.Day,
            new DateOnly(2026, 10, 1), null), Now);

        // Act
        await repository.AddAsync(current);
        var replacement = current.ProposeVehicleChange(vehicleB, new DateOnly(2026, 10, 15), Now.AddDays(14));
        current.Supersede(new DateOnly(2026, 10, 15));
        await repository.UpdateAsync(current);
        await repository.AddAsync(replacement);
        var restarted = new PostgresOperatorAssignmentRepository(postgres.Reopen());

        // Assert
        var history = (await restarted.FindByOperatorIdAsync(operatorId)).ToList();
        Assert.Equal(new[] { replacement.Id, current.Id }, history.Select(a => a.Id));
        Assert.Equal(AssignmentStatus.Superseded, history[1].Status);
        Assert.Equal(new DateOnly(2026, 10, 14), history[1].ValidTo);
        Assert.Equal(new[] { current.Id }, (await restarted.FindByVehicleIdAsync(vehicleA)).Select(a => a.Id));
        Assert.Equal(new[] { replacement.Id }, (await restarted.FindByVehicleIdAsync(vehicleB)).Select(a => a.Id));
    }
}
