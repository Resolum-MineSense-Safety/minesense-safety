using System.Security.Cryptography;
using System.Text;
using IdentityAccessService.Application.Internal.OutboundServices;
using IdentityAccessService.Domain.Model.Aggregates;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace IdentityAccessService.Infrastructure.Tokens;

/// <summary>Issues RS256 bearer tokens with the user's id, role and mining unit scopes (ADR-007).</summary>
public class JwtTokenService(RsaSigningKeyProvider keyProvider, IOptions<JwtOptions> options) : ITokenService
{
    public const string RoleClaim = "role";
    public const string MiningUnitClaim = "mining_unit";

    private readonly JsonWebTokenHandler _handler = new();

    public TimeSpan RefreshTokenLifetime => TimeSpan.FromDays(options.Value.RefreshTokenDays);

    public IssuedAccessToken IssueAccessToken(User user, DateTimeOffset issuedAt)
    {
        var jwt = options.Value;
        var expiresAt = issuedAt.AddMinutes(jwt.AccessTokenMinutes);
        var claims = new Dictionary<string, object>
        {
            [JwtRegisteredClaimNames.Sub] = user.Id.ToString(),
            [JwtRegisteredClaimNames.UniqueName] = user.Username,
            [JwtRegisteredClaimNames.Name] = user.FullName,
            [JwtRegisteredClaimNames.Jti] = Guid.NewGuid().ToString(),
            [RoleClaim] = user.Role.ToString()
        };
        if (user.MiningUnitIds.Count > 0)
            claims[MiningUnitClaim] = user.MiningUnitIds.Select(id => id.ToString()).ToList();

        var token = _handler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = jwt.Issuer,
            Audience = jwt.Audience,
            IssuedAt = issuedAt.UtcDateTime,
            NotBefore = issuedAt.UtcDateTime,
            Expires = expiresAt.UtcDateTime,
            Claims = claims,
            SigningCredentials = new SigningCredentials(keyProvider.SigningKey, SecurityAlgorithms.RsaSha256)
        });
        return new IssuedAccessToken(token, expiresAt);
    }

    /// <summary>256 random bits; the client keeps the token and only its hash is stored.</summary>
    public string GenerateRefreshToken() => Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(32));

    public string HashRefreshToken(string refreshToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));
}
