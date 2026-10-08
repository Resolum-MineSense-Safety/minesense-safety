using DeviceManagementService.Domain.Model.Aggregates;
using DeviceManagementService.Domain.Model.ValueObjects;
using DeviceManagementService.Domain.Repositories;
using MineSenseSafety.Shared.Infrastructure.Persistence.InMemory;

namespace DeviceManagementService.Infrastructure.Persistence.InMemory;

public class InMemoryMonitoringDeviceRepository : InMemoryRepository<MonitoringDevice>, IMonitoringDeviceRepository
{
    public Task<MonitoringDevice?> FindBySerialNumberAsync(string serialNumber) =>
        Task.FromResult(Store.Values.FirstOrDefault(device =>
            string.Equals(device.SerialNumber, serialNumber.Trim(), StringComparison.OrdinalIgnoreCase)));

    public Task<IEnumerable<MonitoringDevice>> FindByOperatorIdAsync(Guid operatorId) =>
        Task.FromResult<IEnumerable<MonitoringDevice>>(
            Store.Values.Where(device => device.AssignedOperatorId == operatorId)
                .OrderBy(device => device.Type)
                .ThenBy(device => device.SerialNumber)
                .ToList());

    public Task<IEnumerable<MonitoringDevice>> FindByStatusAsync(DeviceStatus? status) =>
        Task.FromResult<IEnumerable<MonitoringDevice>>(
            Store.Values.Where(device => status is null || device.Status == status)
                .OrderBy(device => device.SerialNumber)
                .ToList());
}
