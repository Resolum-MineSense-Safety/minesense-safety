using System.Text.RegularExpressions;
using Npgsql;

namespace MineSenseSafety.Shared.Infrastructure.Persistence.PostgreSql;

/// <summary>
/// Database per service (ADR-005): every relational context names its own database in its
/// connection string (minesense_identity, minesense_operations...). On a fresh server (docker
/// compose, a new RDS instance) the database does not exist yet, so the service creates it through
/// the <c>postgres</c> maintenance database before creating its tables. The user needs the
/// CREATEDB privilege for that; otherwise the database must be created beforehand.
/// </summary>
public static partial class PostgresDatabase
{
    public static async Task EnsureCreatedAsync(string connectionString, CancellationToken cancellationToken)
    {
        var target = new NpgsqlConnectionStringBuilder(connectionString);
        if (string.IsNullOrWhiteSpace(target.Database)) return;

        try
        {
            await using var probe = new NpgsqlConnection(connectionString);
            await probe.OpenAsync(cancellationToken);
            return;
        }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.InvalidCatalogName)
        {
            // The database is missing: create it below.
        }

        if (!SafeName().IsMatch(target.Database))
            throw new InvalidOperationException($"Invalid PostgreSQL database name '{target.Database}'.");

        var maintenance = new NpgsqlConnectionStringBuilder(connectionString) { Database = "postgres", Pooling = false };
        await using var connection = new NpgsqlConnection(maintenance.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand($"CREATE DATABASE \"{target.Database}\"", connection);
        try
        {
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (PostgresException exception) when (exception.SqlState is PostgresErrorCodes.DuplicateDatabase
                                                      or PostgresErrorCodes.UniqueViolation)
        {
            // Another replica created it first (a simultaneous CREATE DATABASE fails on the catalog index).
        }
    }

    [GeneratedRegex("^[A-Za-z_][A-Za-z0-9_]{0,62}$")]
    private static partial Regex SafeName();
}
