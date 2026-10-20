using MineSenseSafety.Shared.Domain.Model;
using OperationsService.Domain.Model.Aggregates;
using OperationsService.Domain.Model.Commands;
using OperationsService.Domain.Model.ValueObjects;

namespace OperationsService.Tests.Domain;

public class MiningUnitTests
{
    private static MiningUnit RegisterUnit() =>
        new(new RegisterMiningUnitCommand(" um-antamina ", " Antamina ", " Ancash "));

    [Fact]
    public void Constructor_WithCompleteData_NormalizesBusinessCodeAndTrimsFields()
    {
        // Act
        var miningUnit = RegisterUnit();

        // Assert
        Assert.Equal("UM-ANTAMINA", miningUnit.BusinessCode);
        Assert.Equal("Antamina", miningUnit.Name);
        Assert.Equal("Ancash", miningUnit.Region);
        Assert.Empty(miningUnit.Locations);
    }

    [Fact]
    public void Constructor_WithMissingData_ListsEveryMissingField()
    {
        // Arrange
        var command = new RegisterMiningUnitCommand("", " ", "Ancash");

        // Act
        var exception = Assert.Throws<DomainException>(() => new MiningUnit(command));

        // Assert
        Assert.Equal("Missing mandatory data: BusinessCode, Name.", exception.Message);
    }

    [Fact]
    public void Update_WithValidData_ChangesNameAndRegion()
    {
        // Arrange
        var miningUnit = RegisterUnit();

        // Act
        miningUnit.Update(new UpdateMiningUnitCommand(miningUnit.Id, "Antamina Norte", "Huari"));

        // Assert
        Assert.Equal("Antamina Norte", miningUnit.Name);
        Assert.Equal("Huari", miningUnit.Region);
    }

    [Fact]
    public void Update_WithMissingRegion_ThrowsNamingTheField()
    {
        // Arrange
        var miningUnit = RegisterUnit();

        // Act
        var exception = Assert.Throws<DomainException>(() =>
            miningUnit.Update(new UpdateMiningUnitCommand(miningUnit.Id, "Antamina", "")));

        // Assert
        Assert.Contains("Region", exception.Message);
    }

    [Fact]
    public void AddLocation_NewName_AddsLocation()
    {
        // Arrange
        var miningUnit = RegisterUnit();

        // Act
        var location = miningUnit.AddLocation("Tajo Norte", LocationKind.Pit);

        // Assert
        Assert.Equal(new Location("Tajo Norte", LocationKind.Pit), location);
        Assert.Single(miningUnit.Locations);
    }

    [Fact]
    public void AddLocation_DuplicatedNameIgnoringCase_ThrowsDomainException()
    {
        // Arrange
        var miningUnit = RegisterUnit();
        miningUnit.AddLocation("Chancadora 1", LocationKind.Crusher);

        // Act & Assert
        Assert.Throws<DomainException>(() => miningUnit.AddLocation("CHANCADORA 1", LocationKind.Dump));
        Assert.Single(miningUnit.Locations);
    }

    [Fact]
    public void AddLocation_WithoutName_ThrowsMissingName()
    {
        // Arrange
        var miningUnit = RegisterUnit();

        // Act
        var exception = Assert.Throws<DomainException>(() => miningUnit.AddLocation(" ", LocationKind.Ramp));

        // Assert
        Assert.Equal("Missing mandatory data: Name.", exception.Message);
    }
}
