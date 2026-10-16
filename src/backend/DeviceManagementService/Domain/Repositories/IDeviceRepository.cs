using DeviceManagementService.Domain.Model.Aggregates;
using MineSenseSafety.Shared.Domain.Repositories;

namespace DeviceManagementService.Domain.Repositories;

public interface IDeviceRepository : IBaseRepository<Device>
{
    Task<Device?> FindByDeviceCodeAsync(string deviceCode);
}
