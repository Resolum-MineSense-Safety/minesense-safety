using MineSenseSafety.Shared.Domain.Repositories;
using OperationsService.Domain.Model.Aggregates;

namespace OperationsService.Domain.Repositories;

public interface IVehicleRepository : IBaseRepository<Vehicle>
{
    Task<Vehicle?> FindByCodeAsync(string code);
    Task<IEnumerable<Vehicle>> FindByFleetIdAsync(Guid fleetId);
}
