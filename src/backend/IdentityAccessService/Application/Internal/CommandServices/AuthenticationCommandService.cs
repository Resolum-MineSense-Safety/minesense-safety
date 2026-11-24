using IdentityAccessService.Application.Internal.OutboundServices;
using IdentityAccessService.Domain.Model.Aggregates;
using IdentityAccessService.Domain.Model.Commands;
using IdentityAccessService.Domain.Model.ValueObjects;
using IdentityAccessService.Domain.Repositories;
using IdentityAccessService.Domain.Services;

namespace IdentityAccessService.Application.Internal.CommandServices;

public class AuthenticationCommandService(
    IUserRepository userRepository,
    ISessionRepository sessionRepository,
    IHashingService hashingService,
    ITokenService tokenService,
    TimeProvider timeProvider) : IAuthenticationCommandService
{
    /// <summary>
    /// TS-01: an unknown user, a wrong password and a deactivated user get the same answer
    /// so the response does not reveal which usernames exist.
    /// </summary>
    public async Task<AuthenticatedSession?> Handle(SignInCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Username) || string.IsNullOrEmpty(command.Password)) return null;

        var user = await userRepository.FindByUsernameAsync(command.Username);
        if (user is null || !user.IsActive || !hashingService.VerifyPassword(command.Password, user.PasswordHash))
            return null;

        return await OpenSessionAsync(user, timeProvider.GetUtcNow());
    }

    /// <summary>
    /// TS-02: rotates the refresh token. Presenting a token that was already rotated or revoked
    /// is treated as theft and closes every open session of the user.
    /// </summary>
    public async Task<AuthenticatedSession?> Handle(RefreshSessionCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.RefreshToken)) return null;

        var now = timeProvider.GetUtcNow();
        var session = await sessionRepository.FindByRefreshTokenHashAsync(tokenService.HashRefreshToken(command.RefreshToken));
        if (session is null) return null;

        if (session.IsRevoked)
        {
            await RevokeAllSessionsAsync(session.UserId, now);
            return null;
        }
        if (!session.IsActiveAt(now)) return null;

        var user = await userRepository.FindByIdAsync(session.UserId);
        if (user is null || !user.IsActive)
        {
            session.Revoke(now);
            await sessionRepository.UpdateAsync(session);
            return null;
        }

        var renewed = await OpenSessionAsync(user, now);
        session.Revoke(now, renewed.SessionId);
        await sessionRepository.UpdateAsync(session);
        return renewed;
    }

    public async Task Handle(SignOutCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.RefreshToken)) return;

        var session = await sessionRepository.FindByRefreshTokenHashAsync(tokenService.HashRefreshToken(command.RefreshToken));
        if (session is null || session.IsRevoked) return;

        session.Revoke(timeProvider.GetUtcNow());
        await sessionRepository.UpdateAsync(session);
    }

    private async Task<AuthenticatedSession> OpenSessionAsync(User user, DateTimeOffset now)
    {
        var refreshToken = tokenService.GenerateRefreshToken();
        var session = new Session(user.Id, tokenService.HashRefreshToken(refreshToken), now, tokenService.RefreshTokenLifetime);
        await sessionRepository.AddAsync(session);

        var accessToken = tokenService.IssueAccessToken(user, now);
        return new AuthenticatedSession(user, session.Id, accessToken.Value, accessToken.ExpiresAt,
            refreshToken, session.ExpiresAt);
    }

    private async Task RevokeAllSessionsAsync(Guid userId, DateTimeOffset now)
    {
        foreach (var session in await sessionRepository.FindByUserIdAsync(userId))
        {
            if (session.IsRevoked) continue;
            session.Revoke(now);
            await sessionRepository.UpdateAsync(session);
        }
    }
}
