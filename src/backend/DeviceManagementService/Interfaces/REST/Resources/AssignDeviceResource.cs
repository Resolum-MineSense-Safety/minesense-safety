namespace DeviceManagementService.Interfaces.REST.Resources;

public record AssignDeviceResource(Guid? OperatorId, Guid? VehicleId);
