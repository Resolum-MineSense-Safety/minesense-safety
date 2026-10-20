using OperationsService.Domain.Model.Commands;
using OperationsService.Domain.Model.ValueObjects;
using OperationsService.Interfaces.REST.Resources;

namespace OperationsService.Interfaces.REST.Transform;

public static class CreateAssignmentCommandFromResourceAssembler
{
    public static CreateAssignmentCommand ToCommandFromResource(CreateAssignmentResource resource) =>
        new(resource.OperatorId, resource.VehicleId,
            EnumFromResourceAssembler.Parse<Shift>(resource.Shift, nameof(resource.Shift)),
            resource.ValidFrom, resource.ValidTo);
}
