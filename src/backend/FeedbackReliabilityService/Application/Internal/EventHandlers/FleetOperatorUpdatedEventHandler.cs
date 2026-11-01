using FeedbackReliabilityService.Application.Internal.IntegrationEvents;
using FeedbackReliabilityService.Application.Internal.OutboundServices;
using MineSenseSafety.Shared.Application.IntegrationEvents;

namespace FeedbackReliabilityService.Application.Internal.EventHandlers;

/// <summary>Keeps the operator -> operation copy up to date with the snapshots of Fleet Monitoring.</summary>
public class FleetOperatorUpdatedEventHandler(IOperatorOperationDirectory directory) : IIntegrationEventHandler
{
    public string Topic => IntegrationEventTopics.FleetOperatorUpdated;

    public Task HandleAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        var updated = integrationEvent.GetPayload<FleetOperatorUpdatedEvent>();
        return directory.UpdateAsync(updated.OperatorId, updated.MiningUnitId, updated.UpdatedAt);
    }
}
