using MineSenseSafety.Shared.Application.IntegrationEvents;

namespace IncidentManagementService.Tests.Application;

/// <summary>Keeps the published events so the tests can assert on them without Kafka.</summary>
public class FakeIntegrationEventPublisher : IIntegrationEventPublisher
{
    public List<(string Topic, IntegrationEvent Event, string? Key)> Published { get; } = [];

    public Task PublishAsync(string topic, IntegrationEvent integrationEvent, string? partitionKey = null)
    {
        Published.Add((topic, integrationEvent, partitionKey));
        return Task.CompletedTask;
    }
}
