namespace FleetMonitoringService.Domain.Model.Commands;

public record ReassignOperatorVehicleCommand(Guid OperatorId, string VehicleCode, string Location);
