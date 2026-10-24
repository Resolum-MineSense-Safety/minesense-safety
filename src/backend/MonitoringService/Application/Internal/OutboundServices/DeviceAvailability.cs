namespace MonitoringService.Application.Internal.OutboundServices;

/// <summary>
/// Result of checking whether the devices assigned to an operator allow monitoring to start.
/// </summary>
public record DeviceAvailability(bool CanStartMonitoring, string? Reason)
{
    public static DeviceAvailability Available() => new(true, null);

    public static DeviceAvailability Unavailable(string reason) => new(false, reason);
}
