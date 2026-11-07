namespace IdentityAccessService.Interfaces.REST.Resources;

/// <summary>Result of a successful sign-in or refresh (TS-01, TS-02). ExpiresIn is in seconds.</summary>
public record AuthenticatedUserResource(
    Guid Id,
    string Username,
    string FullName,
    string Role,
    IReadOnlyList<Guid> MiningUnitIds,
    string AccessToken,
    string TokenType,
    int ExpiresIn,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt
);
