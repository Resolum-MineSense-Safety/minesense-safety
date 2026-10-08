using FleetMonitoringService.Domain.Model.ValueObjects;

namespace FleetMonitoringService.Domain.Model.Commands;

public record RegisterMonitoredOperatorCommand(
    Guid OperatorId,
    string FullName,
    string VehicleCode,
    string Fleet,
    Shift Shift,
    string Location);
