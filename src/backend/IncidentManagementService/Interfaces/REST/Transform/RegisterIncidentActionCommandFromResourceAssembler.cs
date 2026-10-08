using IncidentManagementService.Domain.Model.Commands;
using IncidentManagementService.Interfaces.REST.Resources;

namespace IncidentManagementService.Interfaces.REST.Transform;

public static class RegisterIncidentActionCommandFromResourceAssembler
{
    public static RegisterIncidentActionCommand ToCommandFromResource(Guid incidentId, RegisterIncidentActionResource resource) =>
        new(incidentId, resource.SupervisorId, resource.Description, resource.Outcome);
}
