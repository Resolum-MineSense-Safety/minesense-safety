using DeviceManagementService.Domain.Model.Aggregates;
using DeviceManagementService.Interfaces.REST.Resources;

namespace DeviceManagementService.Interfaces.REST.Transform;

public static class DeviceResourceFromEntityAssembler
{
    public static DeviceResource ToResourceFromEntity(Device device) =>
        new(device.Id,
            device.DeviceCode,
            device.DeviceType,
            device.OrganizationId,
            device.AssignedOperatorId,
            device.AssignedVehicleId,
            device.Status?.ToString(),
            device.BatteryLevel?.Percentage,
            device.ReportedAt,
            device.AvailableComponents);
}
