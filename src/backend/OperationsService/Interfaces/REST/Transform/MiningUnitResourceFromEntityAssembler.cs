using OperationsService.Domain.Model.Aggregates;
using OperationsService.Interfaces.REST.Resources;

namespace OperationsService.Interfaces.REST.Transform;

public static class MiningUnitResourceFromEntityAssembler
{
    public static MiningUnitResource ToResourceFromEntity(MiningUnit miningUnit) =>
        new(miningUnit.Id, miningUnit.BusinessCode, miningUnit.Name, miningUnit.Region,
            miningUnit.Locations.Select(location => new LocationResource(location.Name, location.Kind.ToString())).ToList());
}
