using MonitoringService.Domain.Model.Commands;
using MonitoringService.Interfaces.REST.Resources;

namespace MonitoringService.Interfaces.REST.Transform;

public static class StartMonitoringCommandFromResourceAssembler
{
    public static StartMonitoringCommand ToCommandFromResource(StartMonitoringResource resource) =>
        new(resource.OperatorId,
            resource.VehicleCode,
            resource.PreShiftCheckId,
            (resource.AvailableSignals ?? []).Select(SignalTypeFromResourceAssembler.ToSignalType).ToList());
}
