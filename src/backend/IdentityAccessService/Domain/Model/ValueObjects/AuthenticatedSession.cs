using IdentityAccessService.Domain.Model.Aggregates;

namespace IdentityAccessService.Domain.Model.ValueObjects;

/// <summary>Tokens handed to a user after signing in or refreshing a session (TS-01, TS-02).</summary>
public record AuthenticatedSession(
    User User,
    Guid SessionId,
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt);
