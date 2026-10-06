using IncidentManagementService.Domain.Model.Commands;
using IncidentManagementService.Interfaces.REST.Resources;

namespace IncidentManagementService.Interfaces.REST.Transform;

public static class AssignIncidentCommandFromResourceAssembler
{
    public static AssignIncidentCommand ToCommandFromResource(Guid incidentId, AssignIncidentResource resource) =>
        new(incidentId, resource.SupervisorId);
}
