using FleetMonitoringService.Domain.Model.Commands;
using FleetMonitoringService.Domain.Model.ValueObjects;
using FleetMonitoringService.Interfaces.REST.Resources;
using MineSenseSafety.Shared.Domain.Model;

namespace FleetMonitoringService.Interfaces.REST.Transform;

public static class RegisterMonitoredOperatorCommandFromResourceAssembler
{
    public static RegisterMonitoredOperatorCommand ToCommandFromResource(RegisterMonitoredOperatorResource resource)
    {
        if (!Enum.TryParse<Shift>(resource.Shift, ignoreCase: true, out var shift))
            throw new DomainException($"Unknown shift '{resource.Shift}'.");

        return new RegisterMonitoredOperatorCommand(resource.OperatorId, resource.FullName,
            resource.VehicleCode, resource.Fleet, shift, resource.Location);
    }
}
