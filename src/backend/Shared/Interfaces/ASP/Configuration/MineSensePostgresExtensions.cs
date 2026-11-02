using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MineSenseSafety.Shared.Infrastructure.Persistence.PostgreSql;
using Npgsql;

namespace MineSenseSafety.Shared.Interfaces.ASP.Configuration;

/// <summary>
/// PostgreSQL persistence of the relational contexts (ADR-005: Identity &amp; Access, Operations,
/// Device Management and Monitoring), switched on by <c>ConnectionStrings:PostgreSql</c>. Each
/// context keeps its own database, named in its connection string (database per service). When the
/// connection string is empty the services keep their in-memory repositories, so tests and a bare
/// <c>dotnet run</c> need no database.
/// </summary>
public static class MineSensePostgresExtensions
{
    public const string ConnectionStringName = "PostgreSql";

    /// <summary>
    /// Registers the pooled data source of the context. Returns false (and registers nothing) when
    /// PostgreSQL is not configured.
    /// </summary>
    public static bool AddMineSensePostgres(this IServiceCollection services, IConfiguration configuration)
    {
        var connection = configuration.GetConnectionString(ConnectionStringName);
        if (string.IsNullOrWhiteSpace(connection)) return false;

        services.AddSingleton(new PostgresConnection(connection));
        services.AddSingleton(_ => NpgsqlDataSource.Create(connection));
        return true;
    }

    /// <summary>Registers a PostgreSQL repository as a singleton whose table is created at startup.</summary>
    public static IServiceCollection AddPostgresRepository<TRepository, TImplementation>(this IServiceCollection services)
        where TRepository : class
        where TImplementation : class, TRepository, IPostgresTableInitializer
    {
        services.AddSingleton<TImplementation>();
        services.AddSingleton<TRepository>(provider => provider.GetRequiredService<TImplementation>());
        services.AddSingleton<IPostgresTableInitializer>(provider => provider.GetRequiredService<TImplementation>());
        return services;
    }

    /// <summary>
    /// Creates the database of the context when missing, then the tables and indexes of every
    /// registered repository. Does nothing without PostgreSQL.
    /// </summary>
    public static async Task InitializePostgresTablesAsync(this IServiceProvider services,
        CancellationToken cancellationToken)
    {
        var connection = services.GetService<PostgresConnection>();
        if (connection is null) return;

        await PostgresDatabase.EnsureCreatedAsync(connection.ConnectionString, cancellationToken);
        foreach (var initializer in services.GetServices<IPostgresTableInitializer>())
            await initializer.InitializeAsync(cancellationToken);
    }

    /// <summary>Connection string of the context, kept to create its database on first start.</summary>
    private sealed record PostgresConnection(string ConnectionString);
}
