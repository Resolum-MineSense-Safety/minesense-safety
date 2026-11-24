using IncidentManagementService.Domain.Model.Aggregates;
using IncidentManagementService.Domain.Model.Entities;
using IncidentManagementService.Interfaces.REST.Resources;

namespace IncidentManagementService.Interfaces.REST.Transform;

public static class IncidentResourceFromEntityAssembler
{
    public static IncidentResource ToResourceFromEntity(Incident incident) =>
        new(incident.Id, incident.OperatorId, incident.AlertId, incident.Severity.ToString(),
            incident.Status.ToString(), incident.OpenedAt, incident.AssigneeId, incident.AssignedAt,
            incident.EscalationLevel, incident.Resolution, incident.ClosedAt,
            incident.AttentionTime?.TotalSeconds,
            incident.Actions.Select(ToResourceFromEntity).ToList());

    public static IncidentActionResource ToResourceFromEntity(IncidentAction action) =>
        new(action.Id, action.ActionType.ToString(), action.Description, action.Result,
            action.PerformedBy, action.PerformedAt);
}
