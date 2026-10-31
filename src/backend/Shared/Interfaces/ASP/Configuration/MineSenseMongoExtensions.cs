using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MineSenseSafety.Shared.Application.IntegrationEvents;
using MineSenseSafety.Shared.Infrastructure.Messaging;
using MineSenseSafety.Shared.Infrastructure.Persistence.MongoDb;
using MongoDB.Driver;

namespace MineSenseSafety.Shared.Interfaces.ASP.Configuration;

/// <summary>
/// MongoDB persistence of the document contexts (ADR-005), switched on by
/// <c>ConnectionStrings:MongoDb</c>. Each context keeps its own database (<c>MongoDb:Database</c>,
/// e.g. <c>minesense_alerts</c>). When the connection string is empty the services keep their
/// in-memory repositories, so tests and a bare <c>dotnet run</c> need no database.
/// </summary>
public static class MineSenseMongoExtensions
{
    public const string ConnectionStringName = "MongoDb";
    public const string DatabaseKey = "MongoDb:Database";

    /// <summary>
    /// Registers the client, the database of the context and the processed-event store of the Kafka
    /// consumer. Returns false (and registers nothing) when MongoDB is not configured.
    /// </summary>
    public static bool AddMineSenseMongo(this IServiceCollection services, IConfiguration configuration,
        string defaultDatabase)
    {
        var connection = configuration.GetConnectionString(ConnectionStringName);
        if (string.IsNullOrWhiteSpace(connection)) return false;

        services.AddSingleton<IMongoClient>(_ => new MongoClient(connection));
        services.AddSingleton(provider => provider.GetRequiredService<IMongoClient>()
            .GetDatabase(configuration[DatabaseKey] ?? defaultDatabase));
        services.AddSingleton<MongoProcessedEventStore>();
        services.Replace(ServiceDescriptor.Singleton<IProcessedEventStore>(provider =>
            provider.GetRequiredService<MongoProcessedEventStore>()));
        services.AddSingleton<IMongoCollectionInitializer>(provider =>
            provider.GetRequiredService<MongoProcessedEventStore>());
        return true;
    }

    /// <summary>Registers a MongoDB repository as a singleton whose indexes are created at startup.</summary>
    public static IServiceCollection AddMongoRepository<TRepository, TImplementation>(this IServiceCollection services)
        where TRepository : class
        where TImplementation : class, TRepository, IMongoCollectionInitializer
    {
        services.AddSingleton<TImplementation>();
        services.AddSingleton<TRepository>(provider => provider.GetRequiredService<TImplementation>());
        services.AddSingleton<IMongoCollectionInitializer>(provider => provider.GetRequiredService<TImplementation>());
        return services;
    }

    /// <summary>Creates the indexes of every registered collection; does nothing without MongoDB.</summary>
    public static async Task InitializeMongoCollectionsAsync(this IServiceProvider services,
        CancellationToken cancellationToken)
    {
        foreach (var initializer in services.GetServices<IMongoCollectionInitializer>())
            await initializer.InitializeAsync(cancellationToken);
    }
}
