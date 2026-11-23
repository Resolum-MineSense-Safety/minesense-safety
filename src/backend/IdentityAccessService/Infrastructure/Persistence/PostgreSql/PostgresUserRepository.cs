using IdentityAccessService.Domain.Model.Aggregates;
using IdentityAccessService.Domain.Repositories;
using MineSenseSafety.Shared.Infrastructure.Persistence.PostgreSql;
using Npgsql;

namespace IdentityAccessService.Infrastructure.Persistence.PostgreSql;

/// <summary>
/// Users in PostgreSQL (ADR-005). The username is stored already normalized (trimmed, lower case)
/// and protected by a unique index, so two sign-ups racing for the same username cannot both win.
/// </summary>
public class PostgresUserRepository(NpgsqlDataSource dataSource)
    : PostgresRepository<User>(dataSource, "users"), IUserRepository
{
    protected override IReadOnlyList<PostgresColumn<User>> Columns { get; } =
    [
        new("username", "text NOT NULL", user => user.Username),
        new("role", "text NOT NULL", user => user.Role.ToString()),
        new("is_active", "boolean NOT NULL", user => user.IsActive)
    ];

    protected override IEnumerable<string> Indexes() =>
    [
        "CREATE UNIQUE INDEX IF NOT EXISTS ux_users_username ON users (username)"
    ];

    public Task<User?> FindByUsernameAsync(string username) =>
        FindOneAsync("WHERE username = $1", Normalize(username));

    public Task<bool> ExistsByUsernameAsync(string username) =>
        ExistsAsync("username = $1", Normalize(username));

    private static string Normalize(string username) => username.Trim().ToLowerInvariant();
}
