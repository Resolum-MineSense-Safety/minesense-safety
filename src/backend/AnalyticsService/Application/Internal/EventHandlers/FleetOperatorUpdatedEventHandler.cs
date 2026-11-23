using AnalyticsService.Application.Internal.IntegrationEvents;
using AnalyticsService.Domain.Model.ReadModels;
using AnalyticsService.Domain.Repositories;
using MineSenseSafety.Shared.Application.IntegrationEvents;

namespace AnalyticsService.Application.Internal.EventHandlers;

/// <summary>Keeps the local operator context up to date with the snapshots of Fleet Monitoring.</summary>
public class FleetOperatorUpdatedEventHandler(IOperatorContextRepository operatorContextRepository)
    : IIntegrationEventHandler
{
    public string Topic => IntegrationEventTopics.FleetOperatorUpdated;

    public Task HandleAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        var updated = integrationEvent.GetPayload<FleetOperatorUpdatedEvent>();
        return operatorContextRepository.UpsertAsync(new OperatorContext(updated.OperatorId,
            OrUnknown(updated.Shift), OrUnknown(updated.MiningUnitId?.ToString()), OrUnknown(updated.Location),
            OrUnknown(updated.Fleet), updated.UpdatedAt));
    }

    private static string OrUnknown(string? value) =>
        string.IsNullOrWhiteSpace(value) ? OperatorContext.Unknown : value.Trim();
}
