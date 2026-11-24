using IncidentManagementService.Domain.Model.ValueObjects;
using IncidentManagementService.Interfaces.REST.Resources;

namespace IncidentManagementService.Interfaces.REST.Transform;

public static class IncidentEventResourceFromEntityAssembler
{
    public static IncidentEventResource ToResourceFromEntity(IncidentEvent incidentEvent) =>
        new(incidentEvent.Type.ToString(), incidentEvent.ActorId, incidentEvent.OccurredAt, incidentEvent.Detail);
}
