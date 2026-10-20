using IdentityAccessService.Infrastructure.Tokens;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace IdentityAccessService.Interfaces.REST;

/// <summary>
/// Publishes the public signing key (JWKS) so every microservice validates tokens locally (ADR-007).
/// </summary>
[ApiController]
[AllowAnonymous]
public class JwksController(RsaSigningKeyProvider keyProvider) : ControllerBase
{
    [HttpGet("/.well-known/jwks.json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetKeys()
    {
        var jwk = JsonWebKeyConverter.ConvertFromRSASecurityKey(keyProvider.PublicKey);
        return Ok(new
        {
            keys = new[]
            {
                new { kty = jwk.Kty, use = "sig", alg = SecurityAlgorithms.RsaSha256, kid = jwk.Kid, n = jwk.N, e = jwk.E }
            }
        });
    }
}
