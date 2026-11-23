using Microsoft.Extensions.Logging;
using MineSenseSafety.Shared.Application.IntegrationEvents;

namespace MineSenseSafety.Shared.Infrastructure.Messaging;

/// <summary>
/// Publisher used when <c>Kafka:BootstrapServers</c> is not set: it only logs the event, so the
/// in-process tests and a bare <c>dotnet run</c> work without a broker.
/// </summary>
public sealed class LoggingIntegrationEventPublisher(ILogger<LoggingIntegrationEventPublisher> logger)
    : IIntegrationEventPublisher, IConfirmedIntegrationEventPublisher
{
    /// <summary>Nothing to confirm without a broker; publishers that need a real delivery only run with Kafka.</summary>
    public Task PublishConfirmedAsync(string topic, IntegrationEvent integrationEvent, string? partitionKey,
        CancellationToken cancellationToken) => PublishAsync(topic, integrationEvent, partitionKey);

    public Task PublishAsync(string topic, IntegrationEvent integrationEvent, string? partitionKey = null)
    {
        logger.LogDebug("Kafka is not configured; event {EventId} ({Type}) for {Topic} was not sent",
            integrationEvent.EventId, integrationEvent.Type, topic);
        return Task.CompletedTask;
    }
}
