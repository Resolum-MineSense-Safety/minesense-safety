using IdentityAccessService.Domain.Model.Aggregates;
using MineSenseSafety.Shared.Domain.Model;

namespace IdentityAccessService.Tests.Domain;

public class SessionTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 19, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Constructor_WithValidData_IsActiveUntilExpiration()
    {
        // Act
        var session = new Session(Guid.NewGuid(), "hash", Now, TimeSpan.FromDays(7));

        // Assert
        Assert.Equal(Now.AddDays(7), session.ExpiresAt);
        Assert.True(session.IsActiveAt(Now.AddDays(6)));
        Assert.False(session.IsActiveAt(Now.AddDays(7)));
    }

    [Theory]
    [InlineData(false, "hash", 1)]
    [InlineData(true, " ", 1)]
    [InlineData(true, "hash", 0)]
    public void Constructor_WithInvalidData_ThrowsDomainException(bool hasUser, string hash, int lifetimeDays)
    {
        // Act & Assert
        Assert.Throws<DomainException>(() =>
            new Session(hasUser ? Guid.NewGuid() : Guid.Empty, hash, Now, TimeSpan.FromDays(lifetimeDays)));
    }

    [Fact]
    public void Revoke_Twice_KeepsTheFirstRevocation()
    {
        // Arrange
        var session = new Session(Guid.NewGuid(), "hash", Now, TimeSpan.FromDays(7));
        var replacement = Guid.NewGuid();

        // Act
        session.Revoke(Now.AddMinutes(1), replacement);
        session.Revoke(Now.AddMinutes(5));

        // Assert
        Assert.False(session.IsActiveAt(Now.AddMinutes(2)));
        Assert.Equal(Now.AddMinutes(1), session.RevokedAt);
        Assert.Equal(replacement, session.ReplacedBySessionId);
    }
}
