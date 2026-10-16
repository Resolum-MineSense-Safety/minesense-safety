using EdgeSynchronizationService.Domain.Model.Aggregates;
using EdgeSynchronizationService.Domain.Repositories;
using EdgeSynchronizationService.Domain.Services;
using MineSenseSafety.Shared.Domain.Model;

namespace EdgeSynchronizationService.Application.Internal.QueryServices;

public class SynchronizationQueryService(IOutboxEventRepository repository) : ISynchronizationQueryService
{
    public Task<IEnumerable<SyncEvent>> GetByVehicleAsync(string vehicleId)
    {
        if (string.IsNullOrWhiteSpace(vehicleId)) throw new DomainException("A vehicle id is required.");
        return repository.FindByVehicleAsync(vehicleId.Trim());
    }
}
