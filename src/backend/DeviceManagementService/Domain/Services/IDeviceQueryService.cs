using DeviceManagementService.Domain.Model.Aggregates;
using DeviceManagementService.Domain.Model.Queries;
using DeviceManagementService.Domain.Model.ValueObjects;

namespace DeviceManagementService.Domain.Services;

public interface IDeviceQueryService
{
    Task<Device?> Handle(GetDeviceByIdQuery query);
    Task<IEnumerable<Device>> Handle(GetDevicesQuery query);
    Task<IReadOnlyCollection<OperatorAvailability>> Handle(GetOperatorAvailabilityQuery query);
}
