using IncidentManagementService.Domain.Model.Commands;
using IncidentManagementService.Interfaces.REST.Resources;

namespace IncidentManagementService.Interfaces.REST.Transform;

public static class OpenIncidentCommandFromResourceAssembler
{
    public static OpenIncidentCommand ToCommandFromResource(OpenIncidentResource resource) =>
        new(resource.AlertId, resource.OperatorId);
}
