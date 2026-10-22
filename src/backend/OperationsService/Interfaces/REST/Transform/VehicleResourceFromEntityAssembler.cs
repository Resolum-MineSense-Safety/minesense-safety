using OperationsService.Domain.Model.Aggregates;
using OperationsService.Interfaces.REST.Resources;

namespace OperationsService.Interfaces.REST.Transform;

public static class VehicleResourceFromEntityAssembler
{
    public static VehicleResource ToResourceFromEntity(Vehicle vehicle) =>
        new(vehicle.Id, vehicle.Code, vehicle.Model, vehicle.FleetId, vehicle.Status.ToString());
}
