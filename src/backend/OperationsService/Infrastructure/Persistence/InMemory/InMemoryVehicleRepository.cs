using MineSenseSafety.Shared.Infrastructure.Persistence.InMemory;
using OperationsService.Domain.Model.Aggregates;
using OperationsService.Domain.Repositories;

namespace OperationsService.Infrastructure.Persistence.InMemory;

public class InMemoryVehicleRepository : InMemoryRepository<Vehicle>, IVehicleRepository
{
    public Task<Vehicle?> FindByCodeAsync(string code) =>
        Task.FromResult(Store.Values.FirstOrDefault(vehicle =>
            string.Equals(vehicle.Code, code, StringComparison.OrdinalIgnoreCase)));

    public Task<IEnumerable<Vehicle>> FindByFleetIdAsync(Guid fleetId) =>
        Task.FromResult<IEnumerable<Vehicle>>(
            Store.Values.Where(vehicle => vehicle.FleetId == fleetId)
                .OrderBy(vehicle => vehicle.Code)
                .ToList());
}
