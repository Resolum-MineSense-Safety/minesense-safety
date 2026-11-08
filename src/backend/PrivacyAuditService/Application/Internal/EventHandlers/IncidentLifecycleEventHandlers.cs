using MineSenseSafety.Shared.Application.IntegrationEvents;
using PrivacyAuditService.Application.Internal.IntegrationEvents;
using PrivacyAuditService.Domain.Model.ReadModels;
using PrivacyAuditService.Domain.Repositories;

namespace PrivacyAuditService.Application.Internal.EventHandlers;

/// <summary>
/// EP05 -> EP10 (US23): the opening of an incident becomes a step of its audit trail. Only ids,
/// severity and times are kept; the resolution text stays in Incident Management (data minimisation).
/// </summary>
public class IncidentOpenedEventHandler(IIncidentLifecycleRepository lifecycleRepository) : IIntegrationEventHandler
{
    public string Topic => IntegrationEventTopics.IncidentOpened;

    public Task HandleAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        var opened = integrationEvent.GetPayload<IncidentOpenedEvent>();
        return lifecycleRepository.AppendAsync(opened.IncidentId, integrationEvent.EventId,
            new IncidentHistoryEntry(opened.OpenedAt, "IncidentOpened", Guid.Empty,
                $"{opened.Severity} incident opened from alert {opened.AlertId}"));
    }
}

/// <summary>EP05 -> EP10 (US23): the closure of an incident becomes a step of its audit trail.</summary>
public class IncidentClosedEventHandler(IIncidentLifecycleRepository lifecycleRepository) : IIntegrationEventHandler
{
    public string Topic => IntegrationEventTopics.IncidentClosed;

    public Task HandleAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        var closed = integrationEvent.GetPayload<IncidentClosedEvent>();
        return lifecycleRepository.AppendAsync(closed.IncidentId, integrationEvent.EventId,
            new IncidentHistoryEntry(closed.ClosedAt, "IncidentClosed", closed.ClosedBy ?? Guid.Empty,
                $"Incident closed after {closed.ActionCount} documented action(s)"));
    }
}
