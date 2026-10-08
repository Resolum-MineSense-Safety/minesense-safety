using DeviceManagementService.Domain.Model.Aggregates;
using DeviceManagementService.Domain.Model.Queries;
using DeviceManagementService.Domain.Repositories;
using DeviceManagementService.Domain.Services;

namespace DeviceManagementService.Application.Internal.QueryServices;

public class MonitoringDeviceQueryService(IMonitoringDeviceRepository deviceRepository) : IMonitoringDeviceQueryService
{
    public Task<MonitoringDevice?> Handle(GetDeviceByIdQuery query) =>
        deviceRepository.FindByIdAsync(query.DeviceId);

    public Task<IEnumerable<MonitoringDevice>> Handle(GetDevicesByOperatorIdQuery query) =>
        deviceRepository.FindByOperatorIdAsync(query.OperatorId);

    public Task<IEnumerable<MonitoringDevice>> Handle(GetDeviceAvailabilityQuery query) =>
        deviceRepository.FindByStatusAsync(query.Status);
}
