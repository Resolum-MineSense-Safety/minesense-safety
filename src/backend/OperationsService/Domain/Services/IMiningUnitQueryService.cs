using OperationsService.Domain.Model.Aggregates;
using OperationsService.Domain.Model.Queries;

namespace OperationsService.Domain.Services;

public interface IMiningUnitQueryService
{
    Task<IEnumerable<MiningUnit>> Handle(GetMiningUnitsQuery query);
    Task<MiningUnit?> Handle(GetMiningUnitByIdQuery query);
}
