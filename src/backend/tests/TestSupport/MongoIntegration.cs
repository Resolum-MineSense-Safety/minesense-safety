using MongoDB.Driver;

namespace MineSenseSafety.Tests.Support;

/// <summary>
/// Repository tests against a real MongoDB server. They run when <c>MINESENSE_TEST_MONGODB</c> holds a
/// connection string (the MongoDB CI job always sets it) and are skipped otherwise, like the
/// <c>FEEDBACK_TEST_MONGODB</c> tests of Feedback &amp; Reliability.
/// </summary>
public sealed class MongoFactAttribute : FactAttribute
{
    public MongoFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(MongoTestDatabase.Variable)))
            Skip = $"Set {MongoTestDatabase.Variable} to run against a real MongoDB server; the MongoDB CI job always sets it.";
    }
}

/// <summary>Throw-away database with a unique name, dropped when the test ends.</summary>
public sealed class MongoTestDatabase : IAsyncDisposable
{
    public const string Variable = "MINESENSE_TEST_MONGODB";

    private readonly MongoClient _client;
    private readonly string _name;

    public MongoTestDatabase(string prefix)
    {
        _client = new MongoClient(Environment.GetEnvironmentVariable(Variable));
        _name = $"{prefix}_{Guid.NewGuid():N}";
        Database = _client.GetDatabase(_name);
    }

    public IMongoDatabase Database { get; }

    /// <summary>A new client on the same database, as a restarted service process would open.</summary>
    public IMongoDatabase Reopen() =>
        new MongoClient(Environment.GetEnvironmentVariable(Variable)).GetDatabase(_name);

    public async ValueTask DisposeAsync() => await _client.DropDatabaseAsync(_name);
}
