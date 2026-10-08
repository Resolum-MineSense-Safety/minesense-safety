using OperationsService.Domain.Model.Commands;
using OperationsService.Domain.Model.ValueObjects;
using OperationsService.Interfaces.REST.Resources;

namespace OperationsService.Interfaces.REST.Transform;

public static class AddLocationCommandFromResourceAssembler
{
    public static AddLocationCommand ToCommandFromResource(Guid miningUnitId, AddLocationResource resource) =>
        new(miningUnitId, resource.Name ?? string.Empty,
            EnumFromResourceAssembler.Parse<LocationKind>(resource.Kind, nameof(resource.Kind)));
}
