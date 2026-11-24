using DeviceManagementService.Domain.Model.Queries;
using DeviceManagementService.Domain.Model.ValueObjects;
using DeviceManagementService.Domain.Repositories;
using DeviceManagementService.Domain.Services;

namespace DeviceManagementService.Application.Internal.QueryServices;

public class PreShiftAvailabilityQueryService(IDeviceRepository deviceRepository)
    : IPreShiftAvailabilityQueryService
{
    public async Task<PreShiftAvailabilityResult> Handle(
        GetPreShiftAvailabilityQuery query, BatteryThreshold batteryThreshold)
    {
        var devices = await deviceRepository.FindByAssignedOperatorIdAsync(query.OperatorId);
        return PreShiftAvailabilityPolicy.Evaluate(devices, batteryThreshold);
    }
}
