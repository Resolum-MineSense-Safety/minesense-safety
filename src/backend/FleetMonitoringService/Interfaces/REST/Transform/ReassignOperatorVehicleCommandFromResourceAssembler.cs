using FleetMonitoringService.Domain.Model.Commands;
using FleetMonitoringService.Interfaces.REST.Resources;

namespace FleetMonitoringService.Interfaces.REST.Transform;

public static class ReassignOperatorVehicleCommandFromResourceAssembler
{
    public static ReassignOperatorVehicleCommand ToCommandFromResource(
        Guid operatorId, ReassignOperatorVehicleResource resource) =>
        new(operatorId, resource.VehicleCode, resource.Location);
}
