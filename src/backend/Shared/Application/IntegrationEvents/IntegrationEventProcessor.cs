using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MineSenseSafety.Shared.Application.IntegrationEvents;

public enum IntegrationEventOutcome
{
    /// <summary>Every handler of the topic succeeded.</summary>
    Handled,

    /// <summary>The event id was already processed: a redelivery, nothing was done.</summary>
    Duplicate,

    /// <summary>Handlers kept failing after every attempt; the event is skipped (poison message).</summary>
    Skipped
}

/// <summary>
/// Runs the handlers of one event in their own DI scope, at most once per event id, retrying
/// failures with a growing delay. Kept apart from the Kafka loop so it can be tested without a broker.
/// </summary>
public sealed class IntegrationEventProcessor(
    IServiceScopeFactory scopeFactory,
    IProcessedEventStore processedEvents,
    ILogger<IntegrationEventProcessor> logger,
    int maxAttempts = 3,
    TimeSpan? retryDelay = null)
{
    private readonly TimeSpan _retryDelay = retryDelay ?? TimeSpan.FromMilliseconds(500);

    public async Task<IntegrationEventOutcome> ProcessAsync(string topic, IntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        if (processedEvents.HasProcessed(integrationEvent.EventId))
        {
            logger.LogDebug("Event {EventId} from {Topic} already processed", integrationEvent.EventId, topic);
            return IntegrationEventOutcome.Duplicate;
        }

        var delay = _retryDelay;
        for (var attempt = 1; attempt <= Math.Max(1, maxAttempts); attempt++)
        {
            try
            {
                await HandleAsync(topic, integrationEvent, cancellationToken);
                processedEvents.MarkProcessed(integrationEvent.EventId);
                return IntegrationEventOutcome.Handled;
            }
            catch (Exception exception) when (exception is not OperationCanceledException ||
                                              !cancellationToken.IsCancellationRequested)
            {
                logger.LogWarning(exception, "Attempt {Attempt}/{MaxAttempts} failed for event {EventId} ({Type}) from {Topic}",
                    attempt, maxAttempts, integrationEvent.EventId, integrationEvent.Type, topic);
                if (attempt < maxAttempts)
                {
                    await Task.Delay(delay, cancellationToken);
                    delay *= 2;
                }
            }
        }

        logger.LogError("Event {EventId} ({Type}) from {Topic} skipped after {MaxAttempts} attempts",
            integrationEvent.EventId, integrationEvent.Type, topic, maxAttempts);
        processedEvents.MarkProcessed(integrationEvent.EventId);
        return IntegrationEventOutcome.Skipped;
    }

    private async Task HandleAsync(string topic, IntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        using var correlation = IntegrationEventContext.Begin(integrationEvent.CorrelationId);
        using var scope = scopeFactory.CreateScope();
        var handlers = scope.ServiceProvider.GetServices<IIntegrationEventHandler>()
            .Where(handler => handler.Topic == topic);
        foreach (var handler in handlers)
            await handler.HandleAsync(integrationEvent, cancellationToken);
    }
}
