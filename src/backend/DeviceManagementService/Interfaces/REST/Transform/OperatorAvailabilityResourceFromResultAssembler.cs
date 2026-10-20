using DeviceManagementService.Domain.Model.ValueObjects;
using DeviceManagementService.Interfaces.REST.Resources;

namespace DeviceManagementService.Interfaces.REST.Transform;

public static class OperatorAvailabilityResourceFromResultAssembler
{
    public static OperatorAvailabilityResource ToResourceFromResult(
        OperatorAvailability result) =>
        new(result.OperatorId,
            result.Status.ToString(),
            result.Devices
                .Select(DeviceResourceFromEntityAssembler.ToResourceFromEntity)
                .ToList());
}
