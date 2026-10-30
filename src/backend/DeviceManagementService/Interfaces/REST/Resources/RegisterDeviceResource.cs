using System.Text.Json.Serialization;

namespace DeviceManagementService.Interfaces.REST.Resources;

public record RegisterDeviceResource(
    [property: JsonRequired] string DeviceCode,
    [property: JsonRequired] string DeviceType,
    [property: JsonRequired] Guid OrganizationId);
