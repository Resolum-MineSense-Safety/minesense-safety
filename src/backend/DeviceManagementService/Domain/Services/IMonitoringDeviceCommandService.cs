using DeviceManagementService.Domain.Model.Aggregates;
using DeviceManagementService.Domain.Model.Commands;

namespace DeviceManagementService.Domain.Services;

public interface IMonitoringDeviceCommandService
{
    Task<MonitoringDevice> Handle(RegisterDeviceCommand command);
    Task<MonitoringDevice?> Handle(AssignDeviceCommand command);
    Task<MonitoringDevice?> Handle(ReportHeartbeatCommand command);
    Task<MonitoringDevice?> Handle(ReportDisconnectionCommand command);
    Task<MonitoringDevice?> Handle(ReportRecoveryCommand command);
}
