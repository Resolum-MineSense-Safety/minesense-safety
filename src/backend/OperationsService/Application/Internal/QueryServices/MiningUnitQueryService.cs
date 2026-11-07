using OperationsService.Domain.Model.Aggregates;
using OperationsService.Domain.Model.Queries;
using OperationsService.Domain.Repositories;
using OperationsService.Domain.Services;

namespace OperationsService.Application.Internal.QueryServices;

public class MiningUnitQueryService(IMiningUnitRepository miningUnitRepository) : IMiningUnitQueryService
{
    public async Task<IEnumerable<MiningUnit>> Handle(GetMiningUnitsQuery query) =>
        (await miningUnitRepository.ListAsync()).OrderBy(unit => unit.BusinessCode).ToList();

    public Task<MiningUnit?> Handle(GetMiningUnitByIdQuery query) =>
        miningUnitRepository.FindByIdAsync(query.MiningUnitId);
}
