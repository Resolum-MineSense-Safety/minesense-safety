namespace DeviceManagementService.Domain.Model.Commands;

public record RegisterDeviceCommand(string DeviceCode, string DeviceType, Guid OrganizationId);
