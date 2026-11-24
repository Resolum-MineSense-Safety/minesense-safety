using OperationsService.Domain.Model.Commands;
using OperationsService.Interfaces.REST.Resources;

namespace OperationsService.Interfaces.REST.Transform;

public static class UpdateMiningUnitCommandFromResourceAssembler
{
    public static UpdateMiningUnitCommand ToCommandFromResource(Guid miningUnitId, UpdateMiningUnitResource resource) =>
        new(miningUnitId, resource.Name ?? string.Empty, resource.Region ?? string.Empty);
}
