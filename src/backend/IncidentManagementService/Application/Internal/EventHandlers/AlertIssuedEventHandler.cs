using IncidentManagementService.Application.Internal.IntegrationEvents;
using IncidentManagementService.Domain.Model.Commands;
using IncidentManagementService.Domain.Model.ValueObjects;
using IncidentManagementService.Domain.Services;
using MineSenseSafety.Shared.Application.IntegrationEvents;

namespace IncidentManagementService.Application.Internal.EventHandlers;

/// <summary>
/// EP03 -> EP05 (US09): a critical alert opens an incident in the control center. Warning alerts
/// stay in the cabin. Opening is idempotent per alert, so a redelivery returns the same incident.
/// A critical warning synchronized from the Edge also opens one: nobody in the control center knew
/// about it. It is dated at its reception, because attention and escalation times measure the control
/// center, and its history keeps the time of the cabin warning (ADR-004).
/// </summary>
public class AlertIssuedEventHandler(
    IIncidentCommandService incidentCommandService,
    ILogger<AlertIssuedEventHandler> logger) : IIntegrationEventHandler
{
    private const string EdgeOrigin = "Edge";

    public string Topic => IntegrationEventTopics.AlertIssued;

    public async Task HandleAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        var alert = integrationEvent.GetPayload<AlertIssuedEvent>();
        if (!string.Equals(alert.Severity, nameof(IncidentSeverity.Critical), StringComparison.OrdinalIgnoreCase))
            return;

        var cabinWarningIssuedAt = alert.Origin == EdgeOrigin ? alert.IssuedAt : (DateTimeOffset?)null;
        var incident = await incidentCommandService.Handle(
            new OpenIncidentCommand(alert.OperatorId, alert.AlertId, IncidentSeverity.Critical, cabinWarningIssuedAt));
        logger.LogInformation("Incident {IncidentId} opened for critical alert {AlertId}", incident.Id, alert.AlertId);
    }
}
