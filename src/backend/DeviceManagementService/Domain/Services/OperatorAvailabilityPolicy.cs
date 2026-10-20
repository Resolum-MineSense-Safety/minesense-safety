using DeviceManagementService.Domain.Model.Aggregates;
using DeviceManagementService.Domain.Model.ValueObjects;

namespace DeviceManagementService.Domain.Services;

public static class OperatorAvailabilityPolicy
{
    public static IReadOnlyCollection<OperatorAvailability> Evaluate(IEnumerable<Device> devices)
    {
        ArgumentNullException.ThrowIfNull(devices);

        return devices
            .Where(device => device.AssignedOperatorId is not null)
            .GroupBy(device => device.AssignedOperatorId!.Value)
            .Select(group => Evaluate(group.Key, group.ToList()))
            .ToList();
    }

    public static OperatorAvailability Evaluate(Guid operatorId, IReadOnlyCollection<Device> devices)
    {
        var availableCount = devices.Count(device => device.Status == DeviceStatus.Available);

        var status = availableCount == 0
            ? OperatorMonitoringStatus.Unavailable
            : availableCount == devices.Count
                ? OperatorMonitoringStatus.Active
                : OperatorMonitoringStatus.Partial;

        return new OperatorAvailability(operatorId, status, devices);
    }
}
