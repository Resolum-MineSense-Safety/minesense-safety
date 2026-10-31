using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MineSenseSafety.Shared.Application.IntegrationEvents;
using MineSenseSafety.Shared.Infrastructure.Messaging;
using MineSenseSafety.Shared.Infrastructure.Messaging.Kafka;

namespace MineSenseSafety.Shared.Interfaces.ASP.Configuration;

/// <summary>
/// Domain events between microservices over Kafka (ADR-004). Switched on by
/// <c>Kafka:BootstrapServers</c>: when it is set, events go to Kafka and the registered handlers
/// consume their topics; when it is empty, events are only logged and no consumer starts.
/// </summary>
public static class MineSenseEventsExtensions
{
    public static IServiceCollection AddMineSenseEvents(this IServiceCollection services,
        IConfiguration configuration, string serviceName)
    {
        services.AddOptions<KafkaOptions>()
            .Bind(configuration.GetSection(KafkaOptions.SectionName))
            .Configure(options => options.ServiceName = serviceName);

        var kafka = configuration.GetSection(KafkaOptions.SectionName).Get<KafkaOptions>() ?? new KafkaOptions();
        if (kafka.IsConfigured)
            services.TryAddSingleton<IIntegrationEventPublisher, KafkaIntegrationEventPublisher>();
        else
            services.TryAddSingleton<IIntegrationEventPublisher, LoggingIntegrationEventPublisher>();

        services.TryAddSingleton<IProcessedEventStore, InMemoryProcessedEventStore>();
        services.TryAddSingleton(provider =>
        {
            var options = provider.GetRequiredService<IOptions<KafkaOptions>>().Value;
            return new IntegrationEventProcessor(provider.GetRequiredService<IServiceScopeFactory>(),
                provider.GetRequiredService<IProcessedEventStore>(),
                provider.GetRequiredService<ILogger<IntegrationEventProcessor>>(),
                options.MaxAttempts, TimeSpan.FromMilliseconds(options.RetryDelayMilliseconds));
        });
        if (kafka.IsConfigured)
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, KafkaIntegrationEventConsumer>());
        return services;
    }

    /// <summary>Registers a handler; the Kafka consumer subscribes to its topic when Kafka is configured.</summary>
    public static IServiceCollection AddIntegrationEventHandler<THandler>(this IServiceCollection services)
        where THandler : class, IIntegrationEventHandler
    {
        services.AddScoped<IIntegrationEventHandler, THandler>();
        return services;
    }
}
