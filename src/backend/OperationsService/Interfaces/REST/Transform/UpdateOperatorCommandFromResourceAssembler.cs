using OperationsService.Domain.Model.Commands;
using OperationsService.Domain.Model.ValueObjects;
using OperationsService.Interfaces.REST.Resources;

namespace OperationsService.Interfaces.REST.Transform;

public static class UpdateOperatorCommandFromResourceAssembler
{
    public static UpdateOperatorCommand ToCommandFromResource(Guid operatorId, UpdateOperatorResource resource) =>
        new(operatorId, resource.FirstName ?? string.Empty, resource.LastName ?? string.Empty, resource.MiningUnitId,
            EnumFromResourceAssembler.Parse<OperatorStatus>(resource.Status, nameof(resource.Status)));
}
