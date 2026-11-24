namespace MonitoringService.Application.Internal.OutboundServices;

/// <summary>
/// Outbound port towards the Device Management bounded context.
/// </summary>
public interface IDeviceAvailabilityService
{
    Task<DeviceAvailability> CheckAsync(Guid operatorId);
}
