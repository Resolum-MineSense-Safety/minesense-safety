using IncidentManagementService.Application.Internal.IntegrationEvents;
using IncidentManagementService.Domain.Model.Aggregates;
using IncidentManagementService.Domain.Model.Commands;
using IncidentManagementService.Domain.Model.Entities;
using IncidentManagementService.Domain.Model.ValueObjects;
using IncidentManagementService.Domain.Repositories;
using IncidentManagementService.Domain.Services;
using MineSenseSafety.Shared.Application.IntegrationEvents;

namespace IncidentManagementService.Application.Internal.CommandServices;

public class IncidentCommandService(
    IIncidentRepository incidentRepository,
    TimeProvider timeProvider,
    IIntegrationEventPublisher eventPublisher) : IIncidentCommandService
{
    private const string Source = "incident-management-service";

    /// <summary>
    /// Opens the incident for an alert. Opening is idempotent per alert so a
    /// resent event (e.g. after an Edge reconnection) does not duplicate it.
    /// </summary>
    public async Task<Incident> Handle(OpenIncidentCommand command)
    {
        var existing = await incidentRepository.FindByAlertIdAsync(command.AlertId);
        if (existing is not null) return existing;

        var incident = new Incident(command, timeProvider.GetUtcNow());
        await incidentRepository.AddAsync(incident);
        await eventPublisher.PublishAsync(IntegrationEventTopics.IncidentOpened,
            IntegrationEvent.Create(IncidentOpenedEvent.Type, Source, incident.Id,
                new IncidentOpenedEvent(incident.Id, incident.OperatorId, incident.AlertId, incident.Severity.ToString(),
                    incident.OpenedAt), incident.OpenedAt));
        return incident;
    }

    public async Task<Incident?> Handle(AssignIncidentCommand command)
    {
        var incident = await incidentRepository.FindByIdAsync(command.IncidentId);
        if (incident is null) return null;

        incident.Assign(command.AssigneeId, timeProvider.GetUtcNow());
        await incidentRepository.UpdateAsync(incident);
        return incident;
    }

    public async Task<IncidentAction?> Handle(RecordIncidentActionCommand command)
    {
        var incident = await incidentRepository.FindByIdAsync(command.IncidentId);
        if (incident is null) return null;

        var action = incident.RecordAction(command.ActionType, command.Description, command.Result,
            command.PerformedBy, command.PerformedAt);
        await incidentRepository.UpdateAsync(incident);
        return action;
    }

    public async Task<Incident?> Handle(CloseIncidentCommand command)
    {
        var incident = await incidentRepository.FindByIdAsync(command.IncidentId);
        if (incident is null) return null;

        incident.Close(command.Resolution, command.ClosedAt ?? timeProvider.GetUtcNow());
        await incidentRepository.UpdateAsync(incident);
        await eventPublisher.PublishAsync(IntegrationEventTopics.IncidentClosed,
            IntegrationEvent.Create(IncidentClosedEvent.Type, Source, incident.Id,
                new IncidentClosedEvent(incident.Id, incident.OperatorId, incident.AlertId, incident.Severity.ToString(),
                    incident.AssigneeId, incident.Resolution!, incident.Actions.Count, incident.ClosedAt!.Value),
                incident.ClosedAt.Value));
        return incident;
    }

    public async Task<IEnumerable<Incident>> Handle(EscalateOverdueIncidentsCommand command)
    {
        var now = timeProvider.GetUtcNow();
        var pending = await incidentRepository.FindAsync(IncidentStatus.Pending, operatorId: null);
        var overdue = pending.Where(incident => incident.IsEscalationDue(now)).ToList();

        foreach (var incident in overdue)
        {
            incident.Escalate(now);
            await incidentRepository.UpdateAsync(incident);
        }

        return overdue;
    }
}
