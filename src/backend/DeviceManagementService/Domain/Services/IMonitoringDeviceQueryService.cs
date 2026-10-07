using DeviceManagementService.Domain.Model.Aggregates;
using DeviceManagementService.Domain.Model.Queries;

namespace DeviceManagementService.Domain.Services;

public interface IMonitoringDeviceQueryService
{
    Task<MonitoringDevice?> Handle(GetDeviceByIdQuery query);
    Task<IEnumerable<MonitoringDevice>> Handle(GetDevicesByOperatorIdQuery query);
    Task<IEnumerable<MonitoringDevice>> Handle(GetDeviceAvailabilityQuery query);
}
