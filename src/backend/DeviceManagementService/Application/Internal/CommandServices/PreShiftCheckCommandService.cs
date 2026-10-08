using DeviceManagementService.Domain.Model.Aggregates;
using DeviceManagementService.Domain.Model.Commands;
using DeviceManagementService.Domain.Repositories;
using DeviceManagementService.Domain.Services;

namespace DeviceManagementService.Application.Internal.CommandServices;

// Sprint 1 · T02 (Andreow Santiago): device test before the shift (RunPreShiftCheck handler).
// Remaining: ping each device through the Edge gateway; it now uses the last reported status.

public class PreShiftCheckCommandService(
    IMonitoringDeviceRepository deviceRepository,
    IPreShiftCheckRepository preShiftCheckRepository,
    TimeProvider timeProvider) : IPreShiftCheckCommandService
{
    public async Task<PreShiftCheck> Handle(RunPreShiftCheckCommand command)
    {
        var assignedDevices = await deviceRepository.FindByOperatorIdAsync(command.OperatorId);
        var items = PreShiftCheckPolicy.Evaluate(assignedDevices);
        var check = new PreShiftCheck(command.OperatorId, command.VehicleCode, timeProvider.GetUtcNow(), items);

        await preShiftCheckRepository.AddAsync(check);
        return check;
    }
}
