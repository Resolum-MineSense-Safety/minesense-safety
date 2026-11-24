using DeviceManagementService.Domain.Model.Aggregates;
using DeviceManagementService.Domain.Model.Queries;
using DeviceManagementService.Domain.Repositories;
using DeviceManagementService.Domain.Services;

namespace DeviceManagementService.Application.Internal.QueryServices;

public class DeviceQueryService(IDeviceRepository deviceRepository) : IDeviceQueryService
{
    public Task<Device?> Handle(GetDeviceByIdQuery query) =>
        deviceRepository.FindByIdAsync(query.DeviceId);

    public Task<IEnumerable<Device>> Handle(GetDevicesQuery query) =>
        deviceRepository.ListAsync();
}
