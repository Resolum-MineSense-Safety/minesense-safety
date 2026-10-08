namespace DeviceManagementService.Domain.Model.ValueObjects;

/// <summary>Operational availability of a monitoring device.</summary>
public enum DeviceStatus
{
    Available,
    Degraded,
    Disconnected
}
