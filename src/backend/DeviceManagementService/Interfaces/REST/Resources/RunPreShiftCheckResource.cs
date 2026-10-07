namespace DeviceManagementService.Interfaces.REST.Resources;

public record RunPreShiftCheckResource(Guid OperatorId, string VehicleCode);
