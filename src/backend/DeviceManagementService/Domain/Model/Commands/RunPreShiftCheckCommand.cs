namespace DeviceManagementService.Domain.Model.Commands;

public record RunPreShiftCheckCommand(Guid OperatorId, string VehicleCode);
