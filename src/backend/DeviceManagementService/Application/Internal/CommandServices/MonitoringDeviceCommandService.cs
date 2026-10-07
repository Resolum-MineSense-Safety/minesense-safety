using DeviceManagementService.Domain.Model.Aggregates;
using DeviceManagementService.Domain.Model.Commands;
using DeviceManagementService.Domain.Repositories;
using DeviceManagementService.Domain.Services;
using MineSenseSafety.Shared.Domain.Model;

namespace DeviceManagementService.Application.Internal.CommandServices;

public class MonitoringDeviceCommandService(IMonitoringDeviceRepository deviceRepository, TimeProvider timeProvider)
    : IMonitoringDeviceCommandService
{
    public async Task<MonitoringDevice> Handle(RegisterDeviceCommand command)
    {
        var device = new MonitoringDevice(command);
        if (await deviceRepository.FindBySerialNumberAsync(device.SerialNumber) is not null)
            throw new DomainException($"A device with serial number '{device.SerialNumber}' is already registered.");

        await deviceRepository.AddAsync(device);
        return device;
    }

    public Task<MonitoringDevice?> Handle(AssignDeviceCommand command) =>
        UpdateAsync(command.DeviceId, device => device.AssignTo(command.OperatorId, command.VehicleCode));

    public Task<MonitoringDevice?> Handle(ReportHeartbeatCommand command) =>
        UpdateAsync(command.DeviceId, device => device.ReportHeartbeat(command.BatteryLevel, timeProvider.GetUtcNow()));

    // Sprint 1 · T11 (Carlos Onofre): records a disconnection detected during the shift.
    public Task<MonitoringDevice?> Handle(ReportDisconnectionCommand command) =>
        UpdateAsync(command.DeviceId, device => device.ReportDisconnection(timeProvider.GetUtcNow(), command.Reason));

    // Sprint 1 · T11 (Carlos Onofre): records the recovery and closes the open disconnection.
    public Task<MonitoringDevice?> Handle(ReportRecoveryCommand command) =>
        UpdateAsync(command.DeviceId, device => device.ReportRecovery(timeProvider.GetUtcNow()));

    private async Task<MonitoringDevice?> UpdateAsync(Guid deviceId, Action<MonitoringDevice> change)
    {
        var device = await deviceRepository.FindByIdAsync(deviceId);
        if (device is null) return null;

        change(device);
        await deviceRepository.UpdateAsync(device);
        return device;
    }
}
