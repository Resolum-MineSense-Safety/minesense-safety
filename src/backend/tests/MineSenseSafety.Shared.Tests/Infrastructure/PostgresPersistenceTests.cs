using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MineSenseSafety.Shared.Domain.Model;
using MineSenseSafety.Shared.Domain.Repositories;
using MineSenseSafety.Shared.Infrastructure.Persistence.PostgreSql;
using MineSenseSafety.Shared.Interfaces.ASP.Configuration;
using MineSenseSafety.Shared.Tests.Domain.Model;
using MineSenseSafety.Tests.Support;
using Npgsql;

namespace MineSenseSafety.Shared.Tests.Infrastructure;

public interface ISampleRepository : IBaseRepository<SampleAggregate>;

/// <summary>Repository of the sample aggregate, with its name as unique business key.</summary>
public class PostgresSampleRepository(NpgsqlDataSource dataSource)
    : PostgresRepository<SampleAggregate>(dataSource, "samples"), ISampleRepository
{
    protected override IReadOnlyList<PostgresColumn<SampleAggregate>> Columns { get; } =
    [
        new("name", "text NOT NULL", sample => sample.Name),
        new("state", "text NOT NULL", sample => sample.State.ToString()),
        new("at", "timestamptz NOT NULL", sample => Utc(sample.At))
    ];

    protected override IEnumerable<string> Indexes() =>
    [
        "CREATE UNIQUE INDEX IF NOT EXISTS ux_samples_name ON samples (name)"
    ];

    public Task<IReadOnlyList<SampleAggregate>> FindByStateAsync(SampleState state) =>
        QueryAsync("WHERE state = $1 ORDER BY at, id", state.ToString());

    public Task<bool> ExistsByNameAsync(string name) => ExistsAsync("name = $1", name);
}

public class PostgresPersistenceTests
{
    private static readonly DateTimeOffset Start = new(2026, 10, 30, 7, 15, 0, 123, TimeSpan.FromHours(-5));

    private static SampleAggregate Sample(string name, int minutes) =>
        new(name, new SampleValue(0.25, "night"), Start.AddMinutes(minutes).AddTicks(9));

    [Fact]
    public async Task AddMineSensePostgres_WithoutConnectionString_KeepsInMemoryAndSkipsInitialization()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        // Act
        var configured = services.AddMineSensePostgres(configuration);
        var provider = services.BuildServiceProvider();
        await provider.InitializePostgresTablesAsync(CancellationToken.None);

        // Assert
        Assert.False(configured);
        Assert.Null(provider.GetService<NpgsqlDataSource>());
        Assert.Empty(provider.GetServices<IPostgresTableInitializer>());
    }

    [Fact]
    public void AddMineSensePostgres_WithConnectionString_RegistersDataSourceAndRepositories()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:PostgreSql"] = "Host=localhost;Database=minesense_tests;Username=tests"
        }).Build();

        // Act
        var configured = services.AddMineSensePostgres(configuration);
        services.AddPostgresRepository<ISampleRepository, PostgresSampleRepository>();
        var provider = services.BuildServiceProvider();

        // Assert: nothing connects until the tables are initialized.
        Assert.True(configured);
        Assert.Equal("minesense_tests",
            new NpgsqlConnectionStringBuilder(provider.GetRequiredService<NpgsqlDataSource>().ConnectionString).Database);
        Assert.Same(provider.GetRequiredService<ISampleRepository>(),
            Assert.Single(provider.GetServices<IPostgresTableInitializer>()));
    }

    [PostgresFact]
    public async Task Postgres_RealServer_CreatesDatabasePersistsAcrossRestartAndRejectsDuplicates()
    {
        // Arrange
        await using var postgres = new PostgresTestDatabase("shared_tests");
        var repository = await postgres.PrepareAsync(new PostgresSampleRepository(postgres.DataSource));
        var first = Sample("Line 1", 10);
        var second = Sample("Line 2", 5);

        // Act
        await repository.AddAsync(first);
        await repository.AddAsync(second);
        first.Record("checked");
        await repository.UpdateAsync(first);
        var sameName = await Assert.ThrowsAsync<DomainConflictException>(() => repository.AddAsync(Sample("Line 1", 20)));
        var sameId = await Assert.ThrowsAsync<DomainConflictException>(() => repository.AddAsync(first));
        var restarted = await postgres.PrepareAsync(new PostgresSampleRepository(postgres.Reopen()));

        // Assert
        Assert.NotNull(sameName);
        Assert.NotNull(sameId);
        var stored = (await restarted.FindByIdAsync(first.Id))!;
        Assert.Equal(new[] { "checked" }, stored.Entries);
        Assert.Equal(SampleState.Recorded, stored.State);
        Assert.Equal(first.At.Ticks, stored.At.Ticks);
        Assert.Equal(first.At.Offset, stored.At.Offset);
        Assert.Equal(first.Value, stored.Value);
        Assert.Equal(2, (await restarted.ListAsync()).Count());
        Assert.Equal(new[] { second.Id }, (await restarted.FindByStateAsync(SampleState.Created)).Select(s => s.Id));
        Assert.True(await restarted.ExistsByNameAsync("Line 2"));
        Assert.False(await restarted.ExistsByNameAsync("Line 3"));
        Assert.Null(await restarted.FindByIdAsync(Guid.NewGuid()));
    }

    [PostgresFact]
    public async Task Postgres_RealServer_UpdateOfUnknownAggregateInsertsIt()
    {
        // Arrange
        await using var postgres = new PostgresTestDatabase("shared_tests");
        var repository = await postgres.PrepareAsync(new PostgresSampleRepository(postgres.DataSource));
        var sample = Sample("Line 9", 0);

        // Act
        await repository.UpdateAsync(sample);

        // Assert
        Assert.Equal("Line 9", (await repository.FindByIdAsync(sample.Id))!.Name);
    }

    [PostgresFact]
    public async Task Postgres_RealServer_ReplicasStartingTogetherCreateTheSchemaOnce()
    {
        // Arrange
        await using var postgres = new PostgresTestDatabase("shared_tests");

        // Act: three "replicas" create the database and the table at the same time.
        var replicas = Enumerable.Range(0, 3).Select(_ => new PostgresSampleRepository(postgres.Reopen())).ToList();
        await Task.WhenAll(replicas.Select(async replica =>
        {
            await PostgresDatabase.EnsureCreatedAsync(postgres.ConnectionString, CancellationToken.None);
            await replica.InitializeAsync(CancellationToken.None);
        }));
        await replicas[0].AddAsync(Sample("Line 1", 0));

        // Assert
        Assert.Single(await replicas[2].ListAsync());
    }
}
