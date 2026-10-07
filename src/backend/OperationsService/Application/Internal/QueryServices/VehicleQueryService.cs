using OperationsService.Domain.Model.Aggregates;
using OperationsService.Domain.Model.Queries;
using OperationsService.Domain.Repositories;
using OperationsService.Domain.Services;

namespace OperationsService.Application.Internal.QueryServices;

public class VehicleQueryService(IVehicleRepository vehicleRepository) : IVehicleQueryService
{
    public Task<IEnumerable<Vehicle>> Handle(GetVehiclesQuery query) =>
        query.FleetId is { } fleetId
            ? vehicleRepository.FindByFleetIdAsync(fleetId)
            : vehicleRepository.ListAsync();

    public Task<Vehicle?> Handle(GetVehicleByIdQuery query) =>
        vehicleRepository.FindByIdAsync(query.VehicleId);
}
