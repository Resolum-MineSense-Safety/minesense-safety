using DeviceManagementService.Domain.Model.ValueObjects;
using DeviceManagementService.Interfaces.REST.Resources;

namespace DeviceManagementService.Interfaces.REST.Transform;

public static class PreShiftAvailabilityResourceFromResultAssembler
{
    public static PreShiftAvailabilityResource ToResourceFromResult(
        PreShiftAvailabilityResult result) =>
        new(result.Status.ToString(),
            result.Recommendation.ToString(),
            result.EvaluatedDevices
                .Select(DeviceResourceFromEntityAssembler.ToResourceFromEntity)
                .ToList());
}
