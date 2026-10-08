using OperationsService.Domain.Model.Commands;
using OperationsService.Interfaces.REST.Resources;

namespace OperationsService.Interfaces.REST.Transform;

public static class RegisterVehicleCommandFromResourceAssembler
{
    public static RegisterVehicleCommand ToCommandFromResource(RegisterVehicleResource resource) =>
        new(resource.Code ?? string.Empty, resource.Model ?? string.Empty, resource.FleetId);
}
