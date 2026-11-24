using MineSenseSafety.Shared.Infrastructure.Persistence.PostgreSql;
using Npgsql;

namespace MineSenseSafety.Tests.Support;

/// <summary>
/// Repository tests against a real PostgreSQL server. They run when <c>MINESENSE_TEST_POSTGRES</c>
/// holds a connection string of a user that can create databases (the PostgreSQL CI job always
/// sets it) and are skipped otherwise, like the MongoDB tests.
/// </summary>
public sealed class PostgresFactAttribute : FactAttribute
{
    public PostgresFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(PostgresTestDatabase.Variable)))
            Skip = $"Set {PostgresTestDatabase.Variable} to run against a real PostgreSQL server; the PostgreSQL CI job always sets it.";
    }
}

/// <summary>
/// Throw-away database with a unique name, created the way a service creates its own database on
/// first start and dropped when the test ends.
/// </summary>
public sealed class PostgresTestDatabase : IAsyncDisposable
{
    public const string Variable = "MINESENSE_TEST_POSTGRES";

    private readonly List<NpgsqlDataSource> _dataSources = [];
    private readonly string _name;

    public PostgresTestDatabase(string prefix)
    {
        _name = $"{prefix}_{Guid.NewGuid():N}";
        ConnectionString = new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable(Variable))
        {
            Database = _name
        }.ConnectionString;
        DataSource = Reopen();
    }

    public string ConnectionString { get; }

    public NpgsqlDataSource DataSource { get; }

    /// <summary>A new connection pool on the same database, as a restarted service process would open.</summary>
    public NpgsqlDataSource Reopen()
    {
        var dataSource = NpgsqlDataSource.Create(ConnectionString);
        _dataSources.Add(dataSource);
        return dataSource;
    }

    /// <summary>Creates the database (first call) and the table of the repository, as service startup does.</summary>
    public async Task<TRepository> PrepareAsync<TRepository>(TRepository repository)
        where TRepository : IPostgresTableInitializer
    {
        await PostgresDatabase.EnsureCreatedAsync(ConnectionString, CancellationToken.None);
        await repository.InitializeAsync(CancellationToken.None);
        return repository;
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var dataSource in _dataSources)
            await dataSource.DisposeAsync();

        var admin = new NpgsqlConnectionStringBuilder(ConnectionString) { Database = "postgres", Pooling = false };
        await using var connection = new NpgsqlConnection(admin.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{_name}\" WITH (FORCE)", connection);
        await command.ExecuteNonQueryAsync();
    }
}
