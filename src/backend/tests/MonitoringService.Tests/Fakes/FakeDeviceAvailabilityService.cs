using MonitoringService.Application.Internal.OutboundServices;

namespace MonitoringService.Tests.Fakes;

/// <summary>
/// Stands in for the Device Management service: the devices are available for every
/// operator except <see cref="OperatorWithoutDevices"/>.
/// </summary>
public class FakeDeviceAvailabilityService : IDeviceAvailabilityService
{
    public static readonly Guid OperatorWithoutDevices = Guid.Parse("99999999-9999-9999-9999-999999999999");
    public const string UnavailableReason = "The operator's devices are not available to start monitoring.";

    public int Calls { get; private set; }

    public Task<DeviceAvailability> CheckAsync(Guid operatorId)
    {
        Calls++;
        return Task.FromResult(operatorId == OperatorWithoutDevices
            ? DeviceAvailability.Unavailable(UnavailableReason)
            : DeviceAvailability.Available());
    }
}
