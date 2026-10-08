using DeviceManagementService.Domain.Model.Aggregates;
using DeviceManagementService.Interfaces.REST.Resources;

namespace DeviceManagementService.Interfaces.REST.Transform;

public static class PreShiftCheckResourceFromEntityAssembler
{
    public static PreShiftCheckResource ToResourceFromEntity(PreShiftCheck check) =>
        new(check.Id, check.OperatorId, check.VehicleCode, check.CheckedAt, check.Protection.ToString(),
            check.CanStartMonitoring,
            check.Items
                .Select(item => new PreShiftCheckItemResource(item.DeviceType.ToString(), item.SerialNumber,
                    item.Result.ToString(), item.RecommendedAction))
                .ToList());
}
