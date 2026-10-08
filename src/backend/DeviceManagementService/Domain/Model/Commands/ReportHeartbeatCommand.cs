namespace DeviceManagementService.Domain.Model.Commands;

public record ReportHeartbeatCommand(Guid DeviceId, int? BatteryLevel);
