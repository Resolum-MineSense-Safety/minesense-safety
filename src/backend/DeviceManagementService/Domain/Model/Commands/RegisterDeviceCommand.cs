using DeviceManagementService.Domain.Model.ValueObjects;

namespace DeviceManagementService.Domain.Model.Commands;

public record RegisterDeviceCommand(string SerialNumber, DeviceType Type);
