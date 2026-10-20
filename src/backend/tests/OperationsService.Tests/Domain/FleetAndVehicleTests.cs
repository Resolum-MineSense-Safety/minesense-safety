using MineSenseSafety.Shared.Domain.Model;
using OperationsService.Domain.Model.Aggregates;
using OperationsService.Domain.Model.Commands;
using OperationsService.Domain.Model.ValueObjects;

namespace OperationsService.Tests.Domain;

public class FleetAndVehicleTests
{
    [Fact]
    public void Fleet_WithMissingData_ListsMiningUnitIdAndName()
    {
        // Act
        var exception = Assert.Throws<DomainException>(() => new Fleet(new RegisterFleetCommand(Guid.Empty, "")));

        // Assert
        Assert.Equal("Missing mandatory data: MiningUnitId, Name.", exception.Message);
    }

    [Fact]
    public void Fleet_WithCompleteData_KeepsMiningUnit()
    {
        // Arrange
        var miningUnitId = Guid.NewGuid();

        // Act
        var fleet = new Fleet(new RegisterFleetCommand(miningUnitId, " Acarreo 1 "));

        // Assert
        Assert.Equal(miningUnitId, fleet.MiningUnitId);
        Assert.Equal("Acarreo 1", fleet.Name);
    }

    [Fact]
    public void Vehicle_WithCompleteData_StartsActiveWithUpperCaseCode()
    {
        // Act
        var vehicle = new Vehicle(new RegisterVehicleCommand("cat-797f-12", "Caterpillar 797F", Guid.NewGuid()));

        // Assert
        Assert.Equal("CAT-797F-12", vehicle.Code);
        Assert.Equal(VehicleStatus.Active, vehicle.Status);
        Assert.True(vehicle.IsActive);
    }

    [Fact]
    public void Vehicle_WithMissingData_ListsEveryMissingField()
    {
        // Act
        var exception = Assert.Throws<DomainException>(() =>
            new Vehicle(new RegisterVehicleCommand("", "", Guid.Empty)));

        // Assert
        Assert.Equal("Missing mandatory data: Code, Model, FleetId.", exception.Message);
    }

    [Fact]
    public void Vehicle_Update_ChangesModelStatusAndFleet()
    {
        // Arrange
        var vehicle = new Vehicle(new RegisterVehicleCommand("CAT-797F-12", "797F", Guid.NewGuid()));
        var newFleetId = Guid.NewGuid();

        // Act
        vehicle.Update(new UpdateVehicleCommand(vehicle.Id, "797F Tier 4", VehicleStatus.InMaintenance, newFleetId));

        // Assert
        Assert.Equal("797F Tier 4", vehicle.Model);
        Assert.Equal(VehicleStatus.InMaintenance, vehicle.Status);
        Assert.Equal(newFleetId, vehicle.FleetId);
        Assert.False(vehicle.IsActive);
    }

    [Fact]
    public void Vehicle_UpdateWithMissingModel_ThrowsNamingTheField()
    {
        // Arrange
        var vehicle = new Vehicle(new RegisterVehicleCommand("CAT-797F-12", "797F", Guid.NewGuid()));

        // Act
        var exception = Assert.Throws<DomainException>(() =>
            vehicle.Update(new UpdateVehicleCommand(vehicle.Id, "", VehicleStatus.Active, vehicle.FleetId)));

        // Assert
        Assert.Equal("Missing mandatory data: Model.", exception.Message);
    }
}
