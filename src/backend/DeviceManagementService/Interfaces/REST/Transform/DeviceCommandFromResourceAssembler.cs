using DeviceManagementService.Domain.Model.Commands;
using DeviceManagementService.Domain.Model.ValueObjects;
using DeviceManagementService.Interfaces.REST.Resources;
using MineSenseSafety.Shared.Domain.Model;

namespace DeviceManagementService.Interfaces.REST.Transform;

public static class DeviceCommandFromResourceAssembler
{
    public static RegisterDeviceCommand ToRegisterCommand(RegisterDeviceResource resource) =>
        new(resource.DeviceCode, resource.DeviceType, resource.OrganizationId);

    public static AssignDeviceCommand ToAssignCommand(Guid deviceId, AssignDeviceResource resource) =>
        new(deviceId, resource.OperatorId, resource.VehicleId);

    public static ReportDeviceStatusCommand ToReportStatusCommand(
        Guid deviceId, ReportDeviceStatusResource resource)
    {
        if (!Enum.TryParse<DeviceStatus>(resource.Status, true, out var status))
            throw new DomainException($"Unknown device status '{resource.Status}'.");

        return new ReportDeviceStatusCommand(
            deviceId,
            status,
            new BatteryLevel(resource.BatteryLevel),
            resource.ReportedAt,
            resource.AvailableComponents);
    }
}
