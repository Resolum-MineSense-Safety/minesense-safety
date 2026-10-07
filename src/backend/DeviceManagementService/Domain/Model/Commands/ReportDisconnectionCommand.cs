namespace DeviceManagementService.Domain.Model.Commands;

public record ReportDisconnectionCommand(Guid DeviceId, string? Reason);
