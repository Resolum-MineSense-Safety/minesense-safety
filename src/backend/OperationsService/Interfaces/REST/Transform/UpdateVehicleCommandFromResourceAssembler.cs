using OperationsService.Domain.Model.Commands;
using OperationsService.Domain.Model.ValueObjects;
using OperationsService.Interfaces.REST.Resources;

namespace OperationsService.Interfaces.REST.Transform;

public static class UpdateVehicleCommandFromResourceAssembler
{
    public static UpdateVehicleCommand ToCommandFromResource(Guid vehicleId, UpdateVehicleResource resource) =>
        new(vehicleId, resource.Model ?? string.Empty,
            EnumFromResourceAssembler.Parse<VehicleStatus>(resource.Status, nameof(resource.Status)), resource.FleetId);
}
