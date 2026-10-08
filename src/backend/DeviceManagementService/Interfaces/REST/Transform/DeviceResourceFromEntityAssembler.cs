using DeviceManagementService.Domain.Model.Aggregates;
using DeviceManagementService.Interfaces.REST.Resources;

namespace DeviceManagementService.Interfaces.REST.Transform;

public static class DeviceResourceFromEntityAssembler
{
    public static DeviceResource ToResourceFromEntity(MonitoringDevice device) =>
        new(device.Id, device.SerialNumber, device.Type.ToString(), device.AssignedOperatorId, device.VehicleCode,
            device.Status.ToString(), device.BatteryLevel, device.LastSeenAt,
            device.Failures
                .Select(failure => new DeviceFailureResource(failure.Kind, failure.Detail, failure.OccurredAt, failure.RecoveredAt))
                .ToList());
}
