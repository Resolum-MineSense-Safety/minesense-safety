using MonitoringService.Domain.Model.ValueObjects;

namespace MonitoringService.Domain.Model.Commands;

public record StartMonitoringCommand(
    Guid OperatorId,
    string VehicleCode,
    IReadOnlyList<SignalType> AvailableSignals);
