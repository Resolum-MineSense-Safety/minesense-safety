using OperationsService.Domain.Model.Commands;
using OperationsService.Interfaces.REST.Resources;

namespace OperationsService.Interfaces.REST.Transform;

public static class RegisterMiningUnitCommandFromResourceAssembler
{
    public static RegisterMiningUnitCommand ToCommandFromResource(RegisterMiningUnitResource resource) =>
        new(resource.BusinessCode ?? string.Empty, resource.Name ?? string.Empty, resource.Region ?? string.Empty);
}
