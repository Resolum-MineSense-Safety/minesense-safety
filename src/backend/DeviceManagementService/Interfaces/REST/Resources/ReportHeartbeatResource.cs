namespace DeviceManagementService.Interfaces.REST.Resources;

/// <summary>Battery level is null for wired devices.</summary>
public record ReportHeartbeatResource(int? BatteryLevel);
