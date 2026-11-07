using OperationsService.Domain.Model.Aggregates;
using OperationsService.Domain.Model.Queries;
using OperationsService.Domain.Repositories;
using OperationsService.Domain.Services;

namespace OperationsService.Application.Internal.QueryServices;

public class FleetQueryService(IFleetRepository fleetRepository) : IFleetQueryService
{
    public Task<Fleet?> Handle(GetFleetByIdQuery query) =>
        fleetRepository.FindByIdAsync(query.FleetId);

    public Task<IEnumerable<Fleet>> Handle(GetFleetsByMiningUnitQuery query) =>
        query.MiningUnitId is { } miningUnitId
            ? fleetRepository.FindByMiningUnitIdAsync(miningUnitId)
            : fleetRepository.ListAsync();
}
