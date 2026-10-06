using IncidentManagementService.Domain.Model.Aggregates;
using IncidentManagementService.Interfaces.REST.Resources;

namespace IncidentManagementService.Interfaces.REST.Transform;

public static class IncidentResourceFromEntityAssembler
{
    public static IncidentResource ToResourceFromEntity(Incident incident) =>
        new(incident.Id, incident.AlertId, incident.OperatorId, incident.Status.ToString(),
            incident.OpenedAt, incident.AssignedSupervisorId, incident.AssignedAt,
            incident.Actions.Select(action => new IncidentActionResource(
                action.SupervisorId, action.Description, action.Outcome, action.RegisteredAt)).ToList(),
            incident.Resolution, incident.ClosedAt);
}
