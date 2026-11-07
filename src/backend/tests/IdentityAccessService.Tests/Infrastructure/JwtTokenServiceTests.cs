using IdentityAccessService.Domain.Model.Aggregates;
using IdentityAccessService.Domain.Model.ValueObjects;
using IdentityAccessService.Infrastructure.Tokens;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace IdentityAccessService.Tests.Infrastructure;

public class JwtTokenServiceTests
{
    private readonly JwtOptions _options = new() { Issuer = "iam-test", Audience = "services-test", AccessTokenMinutes = 15 };
    private readonly RsaSigningKeyProvider _keys;
    private readonly JwtTokenService _service;

    public JwtTokenServiceTests()
    {
        var options = Options.Create(_options);
        _keys = new RsaSigningKeyProvider(options, NullLogger<RsaSigningKeyProvider>.Instance);
        _service = new JwtTokenService(_keys, options);
    }

    [Fact]
    public async Task IssueAccessToken_ProducesTokenValidWithThePublicKey()
    {
        // Arrange
        var user = new User("jperez", "Juan Perez", UserRole.Supervisor, "hash");
        var miningUnitId = Guid.NewGuid();
        user.GrantMiningUnit(miningUnitId);

        // Act
        var token = _service.IssueAccessToken(user, DateTimeOffset.UtcNow);
        var result = await new JsonWebTokenHandler().ValidateTokenAsync(token.Value, new TokenValidationParameters
        {
            ValidIssuer = "iam-test",
            ValidAudience = "services-test",
            IssuerSigningKey = _keys.PublicKey
        });

        // Assert
        Assert.True(result.IsValid, result.Exception?.Message);
        var jwt = (JsonWebToken)result.SecurityToken;
        Assert.Equal(user.Id.ToString(), jwt.Subject);
        Assert.Equal("Supervisor", jwt.GetClaim(JwtTokenService.RoleClaim).Value);
        Assert.Contains(jwt.Claims, claim => claim.Type == JwtTokenService.MiningUnitClaim && claim.Value == miningUnitId.ToString());
        Assert.Equal(_keys.PublicKey.KeyId, jwt.Kid);
    }

    [Fact]
    public async Task IssueAccessToken_IsRejectedByAnotherKey()
    {
        // Arrange
        var user = new User("jperez", "Juan Perez", UserRole.Operator, "hash");
        var otherKeys = new RsaSigningKeyProvider(Options.Create(_options), NullLogger<RsaSigningKeyProvider>.Instance);

        // Act
        var token = _service.IssueAccessToken(user, DateTimeOffset.UtcNow);
        var result = await new JsonWebTokenHandler().ValidateTokenAsync(token.Value, new TokenValidationParameters
        {
            ValidIssuer = "iam-test",
            ValidAudience = "services-test",
            IssuerSigningKey = otherKeys.PublicKey
        });

        // Assert
        Assert.False(result.IsValid);
    }

    [Fact]
    public void HashRefreshToken_IsDeterministicAndDoesNotExposeTheToken()
    {
        // Arrange
        var refreshToken = _service.GenerateRefreshToken();

        // Act
        var hash = _service.HashRefreshToken(refreshToken);

        // Assert
        Assert.Equal(hash, _service.HashRefreshToken(refreshToken));
        Assert.DoesNotContain(refreshToken, hash);
        Assert.NotEqual(refreshToken, _service.GenerateRefreshToken());
    }
}
