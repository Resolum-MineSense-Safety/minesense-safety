using IdentityAccessService.Domain.Model.Aggregates;
using IdentityAccessService.Domain.Model.ValueObjects;
using MineSenseSafety.Shared.Domain.Model;

namespace IdentityAccessService.Tests.Domain;

public class UserTests
{
    private static User NewOperator() => new("jperez", "Juan Perez", UserRole.Operator, "hash");

    [Fact]
    public void Constructor_WithoutUsername_ThrowsDomainException()
    {
        // Act & Assert
        Assert.Throws<DomainException>(() => new User(" ", "Juan Perez", UserRole.Operator, "hash"));
    }

    [Fact]
    public void Deactivate_ActiveUser_SetsIsActiveToFalse()
    {
        // Arrange
        var user = NewOperator();

        // Act
        user.Deactivate();

        // Assert
        Assert.False(user.IsActive);
    }

    [Fact]
    public void GrantMiningUnit_Twice_KeepsASingleScope()
    {
        // Arrange
        var user = NewOperator();
        var miningUnitId = Guid.NewGuid();

        // Act
        user.GrantMiningUnit(miningUnitId);
        user.GrantMiningUnit(miningUnitId);

        // Assert
        Assert.Single(user.MiningUnitIds);
        Assert.True(user.CanAccessMiningUnit(miningUnitId));
        Assert.False(user.CanAccessMiningUnit(Guid.NewGuid()));
    }

    [Fact]
    public void GrantMiningUnit_WithEmptyId_ThrowsDomainException()
    {
        // Act & Assert
        Assert.Throws<DomainException>(() => NewOperator().GrantMiningUnit(Guid.Empty));
    }

    [Fact]
    public void GrantMiningUnit_ToDeactivatedUser_ThrowsConflict()
    {
        // Arrange
        var user = NewOperator();
        user.Deactivate();

        // Act & Assert
        Assert.Throws<DomainConflictException>(() => user.GrantMiningUnit(Guid.NewGuid()));
    }

    [Fact]
    public void RevokeMiningUnit_RemovesAccess()
    {
        // Arrange
        var user = NewOperator();
        var miningUnitId = Guid.NewGuid();
        user.GrantMiningUnit(miningUnitId);

        // Act
        user.RevokeMiningUnit(miningUnitId);

        // Assert
        Assert.Empty(user.MiningUnitIds);
        Assert.False(user.CanAccessMiningUnit(miningUnitId));
    }

    [Fact]
    public void CanAccessMiningUnit_Administrator_AccessesEveryUnit()
    {
        // Arrange
        var administrator = new User("root", "Root", UserRole.Administrator, "hash");

        // Act & Assert
        Assert.True(administrator.CanAccessMiningUnit(Guid.NewGuid()));
    }
}
