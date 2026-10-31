using Microsoft.Extensions.Logging;
using MineSenseSafety.Shared.Application.IntegrationEvents;

namespace MineSenseSafety.Shared.Infrastructure.Messaging;

/// <summary>
/// Publisher used when <c>Kafka:BootstrapServers</c> is not set: it only logs the event, so the
/// in-process tests and a bare <c>dotnet run</c> work without a broker.
/// </summary>
public sealed class LoggingIntegrationEventPublisher(ILogger<LoggingIntegrationEventPublisher> logger)
    : IIntegrationEventPublisher
{
    public Task PublishAsync(string topic, IntegrationEvent integrationEvent, string? partitionKey = null)
    {
        logger.LogDebug("Kafka is not configured; event {EventId} ({Type}) for {Topic} was not sent",
            integrationEvent.EventId, integrationEvent.Type, topic);
        return Task.CompletedTask;
    }
}
