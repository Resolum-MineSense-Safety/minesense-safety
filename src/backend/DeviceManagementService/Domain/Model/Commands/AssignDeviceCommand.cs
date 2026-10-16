namespace DeviceManagementService.Domain.Model.Commands;

public record AssignDeviceCommand(Guid DeviceId, Guid? OperatorId, Guid? VehicleId);
