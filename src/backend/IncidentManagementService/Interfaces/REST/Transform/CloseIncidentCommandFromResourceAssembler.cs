using IncidentManagementService.Domain.Model.Commands;
using IncidentManagementService.Interfaces.REST.Resources;

namespace IncidentManagementService.Interfaces.REST.Transform;

public static class CloseIncidentCommandFromResourceAssembler
{
    public static CloseIncidentCommand ToCommandFromResource(Guid incidentId, CloseIncidentResource resource) =>
        new(incidentId, resource.Resolution);
}
