using MineSenseSafety.Shared.Domain.Model;
using MineSenseSafety.Shared.Domain.Repositories;
using MineSenseSafety.Shared.Infrastructure.Persistence.MongoDb;
using Npgsql;
using NpgsqlTypes;

namespace MineSenseSafety.Shared.Infrastructure.Persistence.PostgreSql;

/// <summary>Creates the table and indexes of a repository when the service starts.</summary>
public interface IPostgresTableInitializer
{
    Task InitializeAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Column of a repository table besides <c>id</c> and <c>payload</c>: a field its queries filter or
/// sort on, or a business key protected by a unique index. The value is read from the aggregate on
/// every write, so the column always matches the stored state.
/// </summary>
public sealed record PostgresColumn<TAggregate>(string Name, string SqlType, Func<TAggregate, object?> Value);

/// <summary>
/// PostgreSQL adapter of <see cref="IBaseRepository{TAggregate}"/> for the relational contexts
/// (ADR-005, database per service). Each aggregate is one row: the <c>uuid</c> primary key, typed
/// columns for its queries and unique business keys, and the exact <see cref="AggregateSnapshot"/>
/// of its state in a <c>jsonb</c> column, so the domain classes keep their private setters and
/// collections without ORM mappings (the same snapshot the MongoDB repositories store).
/// Add inserts and update upserts by id, like the in-memory repositories they replace; a write
/// that breaks a unique index (e.g. a second user with the same username) is reported as a
/// <see cref="DomainConflictException"/> (HTTP 409), so a racing duplicate never gets stored.
/// </summary>
public abstract class PostgresRepository<TAggregate>(NpgsqlDataSource dataSource, string table)
    : IBaseRepository<TAggregate>, IPostgresTableInitializer
    where TAggregate : AggregateRoot
{
    /// <summary>Serializes schema changes of all the replicas that start at the same time.</summary>
    private const long SchemaLockKey = 0x4D696E6553656E73; // "MineSens"

    protected NpgsqlDataSource DataSource { get; } = dataSource;
    protected string Table { get; } = table;

    /// <summary>Query and business-key columns of the table, in insert order.</summary>
    protected abstract IReadOnlyList<PostgresColumn<TAggregate>> Columns { get; }

    /// <summary><c>CREATE [UNIQUE] INDEX IF NOT EXISTS</c> statements the queries and invariants need.</summary>
    protected virtual IEnumerable<string> Indexes() => [];

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        var columns = string.Concat(Columns.Select(column => $", {column.Name} {column.SqlType}"));
        await using var connection = await DataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await ExecuteAsync(connection, $"SELECT pg_advisory_xact_lock({SchemaLockKey})", cancellationToken);
        await ExecuteAsync(connection,
            $"CREATE TABLE IF NOT EXISTS {Table} (id uuid PRIMARY KEY, payload jsonb NOT NULL{columns})",
            cancellationToken);
        foreach (var index in Indexes())
            await ExecuteAsync(connection, index, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public Task AddAsync(TAggregate aggregate) => WriteAsync(aggregate, InsertSql);

    public virtual Task UpdateAsync(TAggregate aggregate) => WriteAsync(aggregate, UpsertSql);

    public Task<TAggregate?> FindByIdAsync(Guid id) => FindOneAsync("WHERE id = $1", id);

    public async Task<IEnumerable<TAggregate>> ListAsync() => await QueryAsync("ORDER BY id");

    /// <summary>Aggregates of the rows matching <paramref name="clause"/> (positional <c>$1, $2...</c>).</summary>
    protected async Task<IReadOnlyList<TAggregate>> QueryAsync(string clause, params object?[] parameters)
    {
        await using var command = DataSource.CreateCommand($"SELECT payload FROM {Table} {clause}");
        AddParameters(command, parameters);
        await using var reader = await command.ExecuteReaderAsync();
        var aggregates = new List<TAggregate>();
        while (await reader.ReadAsync())
            aggregates.Add(AggregateSnapshot.Deserialize<TAggregate>(reader.GetString(0)));
        return aggregates;
    }

    protected async Task<TAggregate?> FindOneAsync(string clause, params object?[] parameters) =>
        (await QueryAsync($"{clause} LIMIT 1", parameters)).FirstOrDefault();

    protected async Task<bool> ExistsAsync(string condition, params object?[] parameters)
    {
        await using var command = DataSource.CreateCommand($"SELECT EXISTS (SELECT 1 FROM {Table} WHERE {condition})");
        AddParameters(command, parameters);
        return (bool)(await command.ExecuteScalarAsync())!;
    }

    /// <summary>PostgreSQL keeps microseconds: query columns hold UTC times; the payload keeps the exact value.</summary>
    protected static DateTime Utc(DateTimeOffset moment) => moment.UtcDateTime;

    protected static DateTime? Utc(DateTimeOffset? moment) => moment?.UtcDateTime;

    private string InsertSql
    {
        get
        {
            var names = string.Concat(Columns.Select(column => $", {column.Name}"));
            var values = string.Concat(Columns.Select((_, index) => $", ${index + 3}"));
            return $"INSERT INTO {Table} (id, payload{names}) VALUES ($1, $2{values})";
        }
    }

    private string UpsertSql
    {
        get
        {
            var updates = string.Concat(Columns.Select(column => $", {column.Name} = EXCLUDED.{column.Name}"));
            return $"{InsertSql} ON CONFLICT (id) DO UPDATE SET payload = EXCLUDED.payload{updates}";
        }
    }

    private async Task WriteAsync(TAggregate aggregate, string sql)
    {
        await using var command = DataSource.CreateCommand(sql);
        command.Parameters.Add(new NpgsqlParameter { Value = aggregate.Id });
        command.Parameters.Add(new NpgsqlParameter
        {
            NpgsqlDbType = NpgsqlDbType.Jsonb,
            Value = AggregateSnapshot.Serialize(aggregate)
        });
        AddParameters(command, Columns.Select(column => column.Value(aggregate)));
        try
        {
            await command.ExecuteNonQueryAsync();
        }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            throw new DomainConflictException(
                $"A {typeof(TAggregate).Name} with the same identity or business key already exists.");
        }
    }

    private static void AddParameters(NpgsqlCommand command, IEnumerable<object?> values)
    {
        foreach (var value in values)
            command.Parameters.Add(new NpgsqlParameter { Value = value ?? DBNull.Value });
    }

    private static async Task ExecuteAsync(NpgsqlConnection connection, string sql, CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
