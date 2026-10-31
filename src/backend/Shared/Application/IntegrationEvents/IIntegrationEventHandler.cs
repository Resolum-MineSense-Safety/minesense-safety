namespace MineSenseSafety.Shared.Application.IntegrationEvents;

/// <summary>
/// Reacts to the events of one topic. Handlers run without a user token, so they call the
/// application services of their own bounded context in-process, never another service's API.
/// They must be idempotent on their business key: Kafka delivers at least once.
/// </summary>
public interface IIntegrationEventHandler
{
    string Topic { get; }

    Task HandleAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken);
}
