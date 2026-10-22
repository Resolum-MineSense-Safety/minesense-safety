using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace IdentityAccessService.Infrastructure.Tokens;

/// <summary>
/// Holds the RSA key that signs access tokens. Other services only receive the public part
/// through the JWKS endpoint, so they validate tokens without calling this service (ADR-007).
/// </summary>
public sealed class RsaSigningKeyProvider : IDisposable
{
    private readonly RSA _rsa;

    public RsaSigningKeyProvider(IOptions<JwtOptions> options, ILogger<RsaSigningKeyProvider> logger)
    {
        var pem = options.Value.PrivateKeyPem;
        if (string.IsNullOrWhiteSpace(pem))
        {
            _rsa = RSA.Create(2048);
            logger.LogWarning("No signing key configured in {Section}:PrivateKeyPem; using an ephemeral RSA key.",
                JwtOptions.SectionName);
        }
        else
        {
            _rsa = RSA.Create();
            _rsa.ImportFromPem(pem);
        }

        PublicKey = new RsaSecurityKey(_rsa.ExportParameters(includePrivateParameters: false));
        PublicKey.KeyId = Base64UrlEncoder.Encode(PublicKey.ComputeJwkThumbprint());
        SigningKey = new RsaSecurityKey(_rsa) { KeyId = PublicKey.KeyId };
    }

    public RsaSecurityKey SigningKey { get; }

    public RsaSecurityKey PublicKey { get; }

    public void Dispose() => _rsa.Dispose();
}
