using MineSenseSafety.Shared.Application.IntegrationEvents;

namespace EdgeSynchronizationService.Tests.Application;

/// <summary>Keeps the published events and can act as a broker that is down, so no Kafka is needed.</summary>
internal class FakeConfirmedPublisher : IConfirmedIntegrationEventPublisher
{
    public List<(string Topic, IntegrationEvent Event, string? Key)> Published { get; } = [];

    /// <summary>Events accepted before the next ones fail; null means the broker is up.</summary>
    public int? FailAfter { get; set; }

    public Task PublishConfirmedAsync(string topic, IntegrationEvent integrationEvent, string? partitionKey,
        CancellationToken cancellationToken)
    {
        if (FailAfter <= 0) throw new InvalidOperationException("Broker unavailable");
        FailAfter--;
        Published.Add((topic, integrationEvent, partitionKey));
        return Task.CompletedTask;
    }
}

/// <summary>Clock the tests can move forward.</summary>
internal class FakeTimeProvider(DateTimeOffset now) : TimeProvider
{
    public DateTimeOffset Now { get; set; } = now;
    public override DateTimeOffset GetUtcNow() => Now;
}
