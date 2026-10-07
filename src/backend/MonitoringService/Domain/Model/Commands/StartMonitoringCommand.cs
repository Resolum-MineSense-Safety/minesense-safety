using MonitoringService.Domain.Model.ValueObjects;

namespace MonitoringService.Domain.Model.Commands;

public record StartMonitoringCommand(
    Guid OperatorId,
    string VehicleCode,
    Guid? PreShiftCheckId,
    IReadOnlyList<SignalType> AvailableSignals);
