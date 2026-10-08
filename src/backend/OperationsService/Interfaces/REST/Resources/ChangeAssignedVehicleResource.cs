using System.Text.Json.Serialization;

namespace OperationsService.Interfaces.REST.Resources;

public record ChangeAssignedVehicleResource([property: JsonRequired] Guid NewVehicleId, [property: JsonRequired] DateOnly ChangeDate);
