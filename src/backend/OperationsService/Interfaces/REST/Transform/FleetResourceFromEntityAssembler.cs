using OperationsService.Domain.Model.Aggregates;
using OperationsService.Interfaces.REST.Resources;

namespace OperationsService.Interfaces.REST.Transform;

public static class FleetResourceFromEntityAssembler
{
    public static FleetResource ToResourceFromEntity(Fleet fleet) =>
        new(fleet.Id, fleet.MiningUnitId, fleet.Name);
}
