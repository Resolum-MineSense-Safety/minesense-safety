using DeviceManagementService.Domain.Model.Commands;
using DeviceManagementService.Domain.Model.ValueObjects;
using DeviceManagementService.Interfaces.REST.Resources;
using MineSenseSafety.Shared.Domain.Model;

namespace DeviceManagementService.Interfaces.REST.Transform;

public static class RegisterDeviceCommandFromResourceAssembler
{
    public static RegisterDeviceCommand ToCommandFromResource(RegisterDeviceResource resource) =>
        new(resource.SerialNumber, ParseDeviceType(resource.Type));

    private static DeviceType ParseDeviceType(string? value)
    {
        if (!Enum.TryParse<DeviceType>(value, ignoreCase: true, out var type) || !Enum.IsDefined(type)
            || int.TryParse(value, out _))
            throw new DomainException($"Unknown device type '{value}'.");
        return type;
    }
}
