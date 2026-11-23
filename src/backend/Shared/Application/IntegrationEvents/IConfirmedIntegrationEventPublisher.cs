namespace MineSenseSafety.Shared.Application.IntegrationEvents;

/// <summary>
/// Publishes an event and waits for the broker. Unlike <see cref="IIntegrationEventPublisher"/>, whose
/// delivery failures are only logged, this one throws when the broker did not take the event, for
/// producers that keep their events stored and retry them until they are delivered.
/// </summary>
public interface IConfirmedIntegrationEventPublisher
{
    Task PublishConfirmedAsync(string topic, IntegrationEvent integrationEvent, string? partitionKey,
        CancellationToken cancellationToken);
}
