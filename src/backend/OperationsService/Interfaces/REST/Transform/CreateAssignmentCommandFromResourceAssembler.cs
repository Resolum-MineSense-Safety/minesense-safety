using OperationsService.Domain.Model.Commands;
using OperationsService.Domain.Model.ValueObjects;
using OperationsService.Interfaces.REST.Resources;

namespace OperationsService.Interfaces.REST.Transform;

public static class CreateAssignmentCommandFromResourceAssembler
{
    public static CreateAssignmentCommand ToCommandFromResource(CreateAssignmentResource resource) =>
        new(resource.OperatorId, resource.VehicleId,
            EnumFromResourceAssembler.Parse<Shift>(resource.Shift, nameof(resource.Shift)),
            resource.ValidFrom != default ? resource.ValidFrom : ToDate(resource.StartsAt) ?? default,
            resource.ValidTo ?? ToDate(resource.EndsAt));

    private static DateOnly? ToDate(DateTimeOffset? instant) =>
        instant is { } value ? DateOnly.FromDateTime(value.UtcDateTime) : null;
}
