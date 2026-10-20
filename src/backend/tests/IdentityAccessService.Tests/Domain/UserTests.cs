using IdentityAccessService.Domain.Model.Aggregates;
using IdentityAccessService.Domain.Model.ValueObjects;
using MineSenseSafety.Shared.Domain.Model;

namespace IdentityAccessService.Tests.Domain;

public class UserTests
{
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
        var user = new User("jperez", "Juan Perez", UserRole.Operator, "hash");

        // Act
        user.Deactivate();

        // Assert
        Assert.False(user.IsActive);
    }
}
