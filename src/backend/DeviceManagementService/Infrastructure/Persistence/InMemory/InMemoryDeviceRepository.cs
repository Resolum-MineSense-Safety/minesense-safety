using DeviceManagementService.Domain.Model.Aggregates;
using DeviceManagementService.Domain.Repositories;
using MineSenseSafety.Shared.Infrastructure.Persistence.InMemory;

namespace DeviceManagementService.Infrastructure.Persistence.InMemory;

public class InMemoryDeviceRepository : InMemoryRepository<Device>, IDeviceRepository
{
    public Task<Device?> FindByDeviceCodeAsync(string deviceCode) =>
        Task.FromResult(
            Store.Values.FirstOrDefault(device => device.DeviceCode == deviceCode));

    public Task<IEnumerable<Device>> FindByAssignedOperatorIdAsync(Guid operatorId) =>
        Task.FromResult<IEnumerable<Device>>(
            Store.Values.Where(device => device.AssignedOperatorId == operatorId).ToList());
}
