using OperationsService.Domain.Model.Commands;
using OperationsService.Interfaces.REST.Resources;

namespace OperationsService.Interfaces.REST.Transform;

public static class RegisterFleetCommandFromResourceAssembler
{
    public static RegisterFleetCommand ToCommandFromResource(RegisterFleetResource resource) =>
        new(resource.MiningUnitId, resource.Name ?? string.Empty);
}
