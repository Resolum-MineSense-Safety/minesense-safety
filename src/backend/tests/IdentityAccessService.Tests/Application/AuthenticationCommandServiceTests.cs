using IdentityAccessService.Application.Internal.CommandServices;
using IdentityAccessService.Domain.Model.Commands;
using IdentityAccessService.Domain.Model.ValueObjects;
using IdentityAccessService.Infrastructure.Hashing;
using IdentityAccessService.Infrastructure.Persistence.InMemory;
using IdentityAccessService.Infrastructure.Tokens;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace IdentityAccessService.Tests.Application;

public class AuthenticationCommandServiceTests
{
    private const string Password = "S3cure-pass";

    private readonly InMemoryUserRepository _users = new();
    private readonly InMemorySessionRepository _sessions = new();
    private readonly ManualTimeProvider _clock = new(new DateTimeOffset(2026, 10, 19, 8, 0, 0, TimeSpan.Zero));
    private readonly UserCommandService _userService;
    private readonly AuthenticationCommandService _service;

    public AuthenticationCommandServiceTests()
    {
        var hashing = new Pbkdf2HashingService();
        var options = Options.Create(new JwtOptions { AccessTokenMinutes = 15, RefreshTokenDays = 7 });
        var tokens = new JwtTokenService(new RsaSigningKeyProvider(options, NullLogger<RsaSigningKeyProvider>.Instance), options);
        _userService = new UserCommandService(_users, hashing);
        _service = new AuthenticationCommandService(_users, _sessions, hashing, tokens, _clock);
    }

    private Task<IdentityAccessService.Domain.Model.Aggregates.User> SignUpJuanAsync() =>
        _userService.Handle(new SignUpCommand("jperez", "Juan Perez", Password, UserRole.Supervisor));

    [Fact]
    public async Task Handle_SignInWithValidCredentials_ReturnsTokensAndOpensSession()
    {
        // Arrange
        var user = await SignUpJuanAsync();

        // Act
        var session = await _service.Handle(new SignInCommand("JPerez", Password));

        // Assert
        Assert.NotNull(session);
        Assert.Equal(user.Id, session.User.Id);
        Assert.False(string.IsNullOrEmpty(session.AccessToken));
        Assert.False(string.IsNullOrEmpty(session.RefreshToken));
        Assert.Equal(_clock.GetUtcNow().AddMinutes(15), session.AccessTokenExpiresAt);
        Assert.Single(await _sessions.FindByUserIdAsync(user.Id));
    }

    [Theory]
    [InlineData("jperez", "wrong-password")]
    [InlineData("nobody", Password)]
    [InlineData("", "")]
    public async Task Handle_SignInWithInvalidCredentials_ReturnsNull(string username, string password)
    {
        // Arrange
        await SignUpJuanAsync();

        // Act & Assert
        Assert.Null(await _service.Handle(new SignInCommand(username, password)));
    }

    [Fact]
    public async Task Handle_SignInWithInactiveUser_ReturnsNull()
    {
        // Arrange
        var user = await SignUpJuanAsync();
        await _userService.Handle(new DeactivateUserCommand(user.Id));

        // Act & Assert
        Assert.Null(await _service.Handle(new SignInCommand("jperez", Password)));
    }

    [Fact]
    public async Task Handle_Refresh_RotatesTheRefreshToken()
    {
        // Arrange
        await SignUpJuanAsync();
        var signedIn = (await _service.Handle(new SignInCommand("jperez", Password)))!;
        _clock.Advance(TimeSpan.FromMinutes(20));

        // Act
        var refreshed = await _service.Handle(new RefreshSessionCommand(signedIn.RefreshToken));

        // Assert
        Assert.NotNull(refreshed);
        Assert.NotEqual(signedIn.RefreshToken, refreshed.RefreshToken);
        Assert.Equal(_clock.GetUtcNow().AddMinutes(15), refreshed.AccessTokenExpiresAt);
    }

    [Fact]
    public async Task Handle_RefreshWithReusedToken_RevokesEverySessionOfTheUser()
    {
        // Arrange
        await SignUpJuanAsync();
        var signedIn = (await _service.Handle(new SignInCommand("jperez", Password)))!;
        var refreshed = (await _service.Handle(new RefreshSessionCommand(signedIn.RefreshToken)))!;

        // Act
        var reused = await _service.Handle(new RefreshSessionCommand(signedIn.RefreshToken));
        var afterTheft = await _service.Handle(new RefreshSessionCommand(refreshed.RefreshToken));

        // Assert
        Assert.Null(reused);
        Assert.Null(afterTheft);
    }

    [Fact]
    public async Task Handle_RefreshAfterExpiration_ReturnsNull()
    {
        // Arrange
        await SignUpJuanAsync();
        var signedIn = (await _service.Handle(new SignInCommand("jperez", Password)))!;
        _clock.Advance(TimeSpan.FromDays(8));

        // Act & Assert
        Assert.Null(await _service.Handle(new RefreshSessionCommand(signedIn.RefreshToken)));
    }

    [Fact]
    public async Task Handle_RefreshForDeactivatedUser_ReturnsNull()
    {
        // Arrange
        var user = await SignUpJuanAsync();
        var signedIn = (await _service.Handle(new SignInCommand("jperez", Password)))!;
        await _userService.Handle(new DeactivateUserCommand(user.Id));

        // Act & Assert
        Assert.Null(await _service.Handle(new RefreshSessionCommand(signedIn.RefreshToken)));
    }

    [Fact]
    public async Task Handle_SignOut_InvalidatesTheRefreshToken()
    {
        // Arrange
        await SignUpJuanAsync();
        var signedIn = (await _service.Handle(new SignInCommand("jperez", Password)))!;

        // Act
        await _service.Handle(new SignOutCommand(signedIn.RefreshToken));

        // Assert
        Assert.Null(await _service.Handle(new RefreshSessionCommand(signedIn.RefreshToken)));
    }

    private sealed class ManualTimeProvider(DateTimeOffset now) : TimeProvider
    {
        private DateTimeOffset _now = now;

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan delta) => _now += delta;
    }
}
