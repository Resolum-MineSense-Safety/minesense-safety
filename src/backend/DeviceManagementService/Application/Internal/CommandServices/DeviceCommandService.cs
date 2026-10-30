using DeviceManagementService.Domain.Model.Aggregates;
using DeviceManagementService.Domain.Model.Commands;
using DeviceManagementService.Domain.Repositories;
using DeviceManagementService.Domain.Services;
using MineSenseSafety.Shared.Domain.Model;

namespace DeviceManagementService.Application.Internal.CommandServices;

public class DeviceCommandService(IDeviceRepository deviceRepository) : IDeviceCommandService
{
    public async Task<Device> Handle(RegisterDeviceCommand command)
    {
        if (await deviceRepository.FindByDeviceCodeAsync(command.DeviceCode) is not null)
            throw new DomainConflictException("A device with this code already exists.");

        var device = new Device(command);
        await deviceRepository.AddAsync(device);
        return device;
    }

    public async Task<Device?> Handle(AssignDeviceCommand command)
    {
        var device = await deviceRepository.FindByIdAsync(command.DeviceId);
        if (device is null) return null;

        device.Assign(command);
        await deviceRepository.UpdateAsync(device);
        return device;
    }

    public async Task<Device?> Handle(ReportDeviceStatusCommand command)
    {
        var device = await deviceRepository.FindByIdAsync(command.DeviceId);
        if (device is null) return null;

        device.ReportStatus(command);
        await deviceRepository.UpdateAsync(device);
        return device;
    }
}
