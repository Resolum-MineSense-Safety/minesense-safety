using DeviceManagementService.Domain.Model.ValueObjects;
using MineSenseSafety.Shared.Domain.Model;

namespace DeviceManagementService.Interfaces.REST.Transform;

public static class DeviceStatusFromQueryAssembler
{
    /// <summary>Parses the optional ?status= filter; an unknown value breaks the request.</summary>
    public static DeviceStatus? ToStatusFromQuery(string? status)
    {
        if (string.IsNullOrWhiteSpace(status)) return null;
        if (!Enum.TryParse<DeviceStatus>(status, ignoreCase: true, out var parsed) || !Enum.IsDefined(parsed)
            || int.TryParse(status, out _))
            throw new DomainException($"Unknown device status '{status}'.");
        return parsed;
    }
}
