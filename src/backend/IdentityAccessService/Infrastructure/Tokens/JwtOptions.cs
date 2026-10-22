namespace IdentityAccessService.Infrastructure.Tokens;

public class JwtOptions
{
    public const string SectionName = "IdentityAccess:Jwt";

    public string Issuer { get; set; } = "minesense-identity-access";
    public string Audience { get; set; } = "minesense-services";

    /// <summary>Short lifetime: revocation relies on it (ADR-007).</summary>
    public int AccessTokenMinutes { get; set; } = 15;

    public int RefreshTokenDays { get; set; } = 7;

    /// <summary>
    /// RSA private key in PEM format. When empty an ephemeral key is generated at startup,
    /// which is enough for development but invalidates every token on restart.
    /// </summary>
    public string? PrivateKeyPem { get; set; }
}
