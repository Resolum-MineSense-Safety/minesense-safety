using IdentityAccessService.Infrastructure.Hashing;

namespace IdentityAccessService.Tests.Infrastructure;

public class Pbkdf2HashingServiceTests
{
    private readonly Pbkdf2HashingService _hashingService = new();

    [Fact]
    public void VerifyPassword_WithOriginalPassword_ReturnsTrue()
    {
        // Arrange
        var hash = _hashingService.HashPassword("S3cure-pass");

        // Act
        var isValid = _hashingService.VerifyPassword("S3cure-pass", hash);

        // Assert
        Assert.True(isValid);
        Assert.Equal(3, hash.Split('.').Length);
    }

    [Fact]
    public void VerifyPassword_WithWrongPassword_ReturnsFalse()
    {
        // Arrange
        var hash = _hashingService.HashPassword("S3cure-pass");

        // Act
        var isValid = _hashingService.VerifyPassword("wrong-password", hash);

        // Assert
        Assert.False(isValid);
    }
}
