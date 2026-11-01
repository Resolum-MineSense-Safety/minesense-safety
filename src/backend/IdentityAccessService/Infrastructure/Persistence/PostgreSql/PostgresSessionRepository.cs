using IdentityAccessService.Domain.Model.Aggregates;
using IdentityAccessService.Domain.Repositories;
using MineSenseSafety.Shared.Infrastructure.Persistence.PostgreSql;
using Npgsql;

namespace IdentityAccessService.Infrastructure.Persistence.PostgreSql;

/// <summary>
/// Refresh-token sessions in PostgreSQL (ADR-005). Only the hash of the refresh token is stored;
/// a unique index on it makes every token identify exactly one session (TS-02 rotation).
/// </summary>
public class PostgresSessionRepository(NpgsqlDataSource dataSource)
    : PostgresRepository<Session>(dataSource, "sessions"), ISessionRepository
{
    protected override IReadOnlyList<PostgresColumn<Session>> Columns { get; } =
    [
        new("user_id", "uuid NOT NULL", session => session.UserId),
        new("refresh_token_hash", "text NOT NULL", session => session.RefreshTokenHash),
        new("created_at", "timestamptz NOT NULL", session => Utc(session.CreatedAt)),
        new("expires_at", "timestamptz NOT NULL", session => Utc(session.ExpiresAt)),
        new("revoked_at", "timestamptz", session => Utc(session.RevokedAt))
    ];

    protected override IEnumerable<string> Indexes() =>
    [
        "CREATE UNIQUE INDEX IF NOT EXISTS ux_sessions_refresh_token_hash ON sessions (refresh_token_hash)",
        "CREATE INDEX IF NOT EXISTS ix_sessions_user_id ON sessions (user_id, created_at)"
    ];

    public Task<Session?> FindByRefreshTokenHashAsync(string refreshTokenHash) =>
        FindOneAsync("WHERE refresh_token_hash = $1", refreshTokenHash);

    public async Task<IEnumerable<Session>> FindByUserIdAsync(Guid userId) =>
        await QueryAsync("WHERE user_id = $1 ORDER BY created_at, id", userId);
}
