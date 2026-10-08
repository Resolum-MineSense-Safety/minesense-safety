using System.Text.Json.Serialization;

namespace DeviceManagementService.Interfaces.REST.Resources;

public record RunPreShiftCheckResource([property: JsonRequired] Guid OperatorId, string VehicleCode);
