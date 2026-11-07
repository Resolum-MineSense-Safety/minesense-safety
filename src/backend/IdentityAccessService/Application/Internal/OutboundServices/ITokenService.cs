using IdentityAccessService.Domain.Model.Aggregates;

namespace IdentityAccessService.Application.Internal.OutboundServices;

/// <summary>Outbound port that issues signed access tokens and opaque refresh tokens (ADR-007).</summary>
public interface ITokenService
{
    TimeSpan RefreshTokenLifetime { get; }

    IssuedAccessToken IssueAccessToken(User user, DateTimeOffset issuedAt);

    string GenerateRefreshToken();

    string HashRefreshToken(string refreshToken);
}

public record IssuedAccessToken(string Value, DateTimeOffset ExpiresAt);
