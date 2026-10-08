namespace MonitoringService.Interfaces.REST.Resources;

public record StartMonitoringResource(
    Guid OperatorId,
    string VehicleCode,
    Guid? PreShiftCheckId,
    IReadOnlyList<string>? AvailableSignals);
