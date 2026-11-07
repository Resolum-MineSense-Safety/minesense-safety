using DeviceManagementService.Domain.Model.Queries;
using DeviceManagementService.Domain.Model.ValueObjects;

namespace DeviceManagementService.Domain.Services;

public interface IPreShiftAvailabilityQueryService
{
    Task<PreShiftAvailabilityResult> Handle(
        GetPreShiftAvailabilityQuery query, BatteryThreshold batteryThreshold);
}
