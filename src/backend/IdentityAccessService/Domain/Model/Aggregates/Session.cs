using MineSenseSafety.Shared.Domain.Model;

namespace IdentityAccessService.Domain.Model.Aggregates;

/// <summary>
/// Signed-in session backed by a refresh token (TS-02). Only the hash of the refresh token is
/// stored; every refresh rotates it, so a revoked token presented again reveals a stolen token.
/// </summary>
public class Session : AggregateRoot
{
    public Guid UserId { get; private set; }
    public string RefreshTokenHash { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public Guid? ReplacedBySessionId { get; private set; }

    public Session(Guid userId, string refreshTokenHash, DateTimeOffset createdAt, TimeSpan lifetime)
    {
        if (userId == Guid.Empty)
            throw new DomainException("A session must belong to a user.");
        if (string.IsNullOrWhiteSpace(refreshTokenHash))
            throw new DomainException("A session must have a refresh token hash.");
        if (lifetime <= TimeSpan.Zero)
            throw new DomainException("A session must have a positive lifetime.");

        UserId = userId;
        RefreshTokenHash = refreshTokenHash;
        CreatedAt = createdAt;
        ExpiresAt = createdAt + lifetime;
    }

    public bool IsRevoked => RevokedAt is not null;

    public bool IsActiveAt(DateTimeOffset now) => !IsRevoked && now < ExpiresAt;

    /// <summary>Ends the session. Revoking an already revoked session keeps the first revocation.</summary>
    public void Revoke(DateTimeOffset revokedAt, Guid? replacedBySessionId = null)
    {
        if (IsRevoked) return;

        RevokedAt = revokedAt;
        ReplacedBySessionId = replacedBySessionId;
    }
}
