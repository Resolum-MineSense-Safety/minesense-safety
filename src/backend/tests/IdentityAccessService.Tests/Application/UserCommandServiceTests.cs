using IdentityAccessService.Application.Internal.CommandServices;
using IdentityAccessService.Domain.Model.Commands;
using IdentityAccessService.Domain.Model.ValueObjects;
using IdentityAccessService.Infrastructure.Hashing;
using IdentityAccessService.Infrastructure.Persistence.InMemory;
using MineSenseSafety.Shared.Domain.Model;

namespace IdentityAccessService.Tests.Application;

public class UserCommandServiceTests
{
    private const string Password = "S3cure-pass";

    private readonly InMemoryUserRepository _repository = new();
    private readonly UserCommandService _service;

    public UserCommandServiceTests()
    {
        _service = new UserCommandService(_repository, new Pbkdf2HashingService());
    }

    private static SignUpCommand SignUpJuan() =>
        new("JPerez", "Juan Perez", Password, UserRole.Operator);

    [Fact]
    public async Task Handle_SignUpCommand_PersistsActiveUserWithLowerCasedUsername()
    {
        // Arrange
        var command = SignUpJuan();

        // Act
        var user = await _service.Handle(command);

        // Assert
        Assert.NotNull(await _repository.FindByIdAsync(user.Id));
        Assert.Equal("jperez", user.Username);
        Assert.True(user.IsActive);
        Assert.NotEqual(Password, user.PasswordHash);
    }

    [Fact]
    public async Task Handle_SignUpWithDuplicateUsername_ThrowsDomainException()
    {
        // Arrange
        await _service.Handle(SignUpJuan());
        var duplicate = new SignUpCommand("jperez", "Other Person", Password, UserRole.Supervisor);

        // Act & Assert
        await Assert.ThrowsAsync<DomainException>(() => _service.Handle(duplicate));
    }

    [Fact]
    public async Task Handle_SignUpWithShortPassword_ThrowsDomainException()
    {
        // Arrange
        var command = new SignUpCommand("jperez", "Juan Perez", "short", UserRole.Operator);

        // Act & Assert
        await Assert.ThrowsAsync<DomainException>(() => _service.Handle(command));
    }

    [Fact]
    public async Task Handle_SignInWithValidCredentials_ReturnsUser()
    {
        // Arrange
        var registered = await _service.Handle(SignUpJuan());

        // Act
        var user = await _service.Handle(new SignInCommand("jperez", Password));

        // Assert
        Assert.Equal(registered.Id, user.Id);
    }

    [Fact]
    public async Task Handle_SignInWithWrongPassword_ThrowsInvalidCredentials()
    {
        // Arrange
        await _service.Handle(SignUpJuan());

        // Act
        var exception = await Assert.ThrowsAsync<DomainException>(
            () => _service.Handle(new SignInCommand("jperez", "wrong-password")));

        // Assert
        Assert.Equal("Invalid credentials.", exception.Message);
    }

    [Fact]
    public async Task Handle_SignInWithInactiveUser_ThrowsInvalidCredentials()
    {
        // Arrange
        var user = await _service.Handle(SignUpJuan());
        await _service.Handle(new DeactivateUserCommand(user.Id));

        // Act
        var exception = await Assert.ThrowsAsync<DomainException>(
            () => _service.Handle(new SignInCommand("jperez", Password)));

        // Assert
        Assert.Equal("Invalid credentials.", exception.Message);
    }

    [Fact]
    public async Task Handle_ChangeUserRoleCommand_UpdatesRole()
    {
        // Arrange
        var user = await _service.Handle(SignUpJuan());

        // Act
        var updated = await _service.Handle(new ChangeUserRoleCommand(user.Id, UserRole.SafetyManager));

        // Assert
        Assert.NotNull(updated);
        Assert.Equal(UserRole.SafetyManager, updated.Role);
    }
}
