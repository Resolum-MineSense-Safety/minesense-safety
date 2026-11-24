using DeviceManagementService.Domain.Model.Aggregates;
using DeviceManagementService.Domain.Model.Commands;

namespace DeviceManagementService.Domain.Services;

public interface IDeviceCommandService
{
    Task<Device> Handle(RegisterDeviceCommand command);
    Task<Device?> Handle(AssignDeviceCommand command);
    Task<Device?> Handle(ReportDeviceStatusCommand command);
}
