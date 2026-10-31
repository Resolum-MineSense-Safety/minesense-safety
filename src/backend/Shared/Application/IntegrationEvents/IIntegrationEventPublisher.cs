namespace MineSenseSafety.Shared.Application.IntegrationEvents;

/// <summary>
/// Publishes a domain event for the other bounded contexts. Publishing never fails the use case
/// that produced the fact: delivery problems are logged by the implementation.
/// </summary>
public interface IIntegrationEventPublisher
{
    /// <param name="topic">One of <see cref="IntegrationEventTopics"/>.</param>
    /// <param name="integrationEvent">Envelope to send.</param>
    /// <param name="partitionKey">Kafka key; defaults to the aggregate id of the envelope.</param>
    Task PublishAsync(string topic, IntegrationEvent integrationEvent, string? partitionKey = null);
}
