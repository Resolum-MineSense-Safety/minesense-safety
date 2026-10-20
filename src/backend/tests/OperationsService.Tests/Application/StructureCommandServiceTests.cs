using MineSenseSafety.Shared.Domain.Model;
using OperationsService.Domain.Model.Commands;
using OperationsService.Domain.Model.Queries;
using OperationsService.Domain.Model.ValueObjects;

namespace OperationsService.Tests.Application;

public class StructureCommandServiceTests
{
    private readonly OperationsTestContext _context = new();

    [Fact]
    public async Task RegisterMiningUnit_DuplicatedBusinessCode_ReportsTheCoincidence()
    {
        // Arrange
        var existing = await _context.MiningUnitCommands.Handle(new RegisterMiningUnitCommand("UM-01", "Antamina", "Ancash"));

        // Act
        var exception = await Assert.ThrowsAsync<DomainException>(() =>
            _context.MiningUnitCommands.Handle(new RegisterMiningUnitCommand("um-01", "Otra", "Lima")));

        // Assert
        Assert.Contains("UM-01", exception.Message);
        Assert.Contains(existing.Id.ToString(), exception.Message);
        Assert.Single(await _context.MiningUnitQueries.Handle(new GetMiningUnitsQuery()));
    }

    [Fact]
    public async Task UpdateMiningUnitAndAddLocation_UnknownUnit_ReturnNull()
    {
        // Act
        var updated = await _context.MiningUnitCommands.Handle(new UpdateMiningUnitCommand(Guid.NewGuid(), "A", "B"));
        var located = await _context.MiningUnitCommands.Handle(new AddLocationCommand(Guid.NewGuid(), "Tajo", LocationKind.Pit));

        // Assert
        Assert.Null(updated);
        Assert.Null(located);
    }

    [Fact]
    public async Task AddLocation_ExistingUnit_PersistsLocation()
    {
        // Arrange
        var miningUnit = await _context.MiningUnitCommands.Handle(new RegisterMiningUnitCommand("UM-02", "Toquepala", "Tacna"));

        // Act
        await _context.MiningUnitCommands.Handle(new AddLocationCommand(miningUnit.Id, "Botadero Sur", LocationKind.Dump));

        // Assert
        var stored = await _context.MiningUnitQueries.Handle(new GetMiningUnitByIdQuery(miningUnit.Id));
        Assert.Equal("Botadero Sur", Assert.Single(stored!.Locations).Name);
    }

    [Fact]
    public async Task RegisterFleet_UnknownMiningUnit_IsRejected()
    {
        // Act
        var exception = await Assert.ThrowsAsync<DomainException>(() =>
            _context.FleetCommands.Handle(new RegisterFleetCommand(Guid.NewGuid(), "Acarreo 1")));

        // Assert
        Assert.Contains("Mining unit", exception.Message);
    }

    [Fact]
    public async Task RegisterFleet_DuplicatedNameInSameUnit_IsRejected()
    {
        // Arrange
        var fleet = await _context.RegisterFleetAsync();

        // Act & Assert
        await Assert.ThrowsAsync<DomainException>(() =>
            _context.FleetCommands.Handle(new RegisterFleetCommand(fleet.MiningUnitId, "ACARREO 1")));
        Assert.Single(await _context.FleetQueries.Handle(new GetFleetsByMiningUnitQuery(fleet.MiningUnitId)));
    }

    [Fact]
    public async Task RegisterVehicle_UnknownFleet_IsRejected()
    {
        // Act
        var exception = await Assert.ThrowsAsync<DomainException>(() =>
            _context.VehicleCommands.Handle(new RegisterVehicleCommand("CAT-797F-12", "797F", Guid.NewGuid())));

        // Assert
        Assert.Contains("Fleet", exception.Message);
    }

    [Fact]
    public async Task RegisterVehicle_DuplicatedCode_IsRejected()
    {
        // Arrange
        var fleet = await _context.RegisterFleetAsync();
        await _context.VehicleCommands.Handle(new RegisterVehicleCommand("CAT-797F-12", "797F", fleet.Id));

        // Act
        var exception = await Assert.ThrowsAsync<DomainException>(() =>
            _context.VehicleCommands.Handle(new RegisterVehicleCommand("cat-797f-12", "797F", fleet.Id)));

        // Assert
        Assert.Contains("CAT-797F-12", exception.Message);
    }

    [Fact]
    public async Task UpdateVehicle_ToUnknownFleet_IsRejectedAndUnknownVehicleReturnsNull()
    {
        // Arrange
        var vehicle = await _context.RegisterVehicleAsync();

        // Act
        var missing = await _context.VehicleCommands.Handle(
            new UpdateVehicleCommand(Guid.NewGuid(), "797F", VehicleStatus.Active, vehicle.FleetId));

        // Assert
        Assert.Null(missing);
        await Assert.ThrowsAsync<DomainException>(() => _context.VehicleCommands.Handle(
            new UpdateVehicleCommand(vehicle.Id, "797F", VehicleStatus.Active, Guid.NewGuid())));
    }

    [Fact]
    public async Task UpdateVehicle_ToExistingFleet_MovesVehicle()
    {
        // Arrange
        var vehicle = await _context.RegisterVehicleAsync();
        var otherFleet = await _context.RegisterFleetAsync();

        // Act
        await _context.VehicleCommands.Handle(
            new UpdateVehicleCommand(vehicle.Id, "797F", VehicleStatus.InMaintenance, otherFleet.Id));

        // Assert
        var vehicles = await _context.VehicleQueries.Handle(new GetVehiclesQuery(otherFleet.Id));
        Assert.Equal(VehicleStatus.InMaintenance, Assert.Single(vehicles).Status);
    }
}
