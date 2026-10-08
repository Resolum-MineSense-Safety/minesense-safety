using System.Text.Json.Serialization;

namespace DeviceManagementService.Interfaces.REST.Resources;

public record AssignDeviceResource([property: JsonRequired] Guid OperatorId, string VehicleCode);
