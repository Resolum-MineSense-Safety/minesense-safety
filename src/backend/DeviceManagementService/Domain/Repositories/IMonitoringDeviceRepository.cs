using DeviceManagementService.Domain.Model.Aggregates;
using DeviceManagementService.Domain.Model.ValueObjects;
using MineSenseSafety.Shared.Domain.Repositories;

namespace DeviceManagementService.Domain.Repositories;

public interface IMonitoringDeviceRepository : IBaseRepository<MonitoringDevice>
{
    Task<MonitoringDevice?> FindBySerialNumberAsync(string serialNumber);
    Task<IEnumerable<MonitoringDevice>> FindByOperatorIdAsync(Guid operatorId);
    Task<IEnumerable<MonitoringDevice>> FindByStatusAsync(DeviceStatus? status);
}
