using System.Text.Json.Serialization;

namespace OperationsService.Interfaces.REST.Resources;

public record CreateAssignmentResource([property: JsonRequired] Guid OperatorId, [property: JsonRequired] Guid VehicleId, string? Shift, [property: JsonRequired] DateOnly ValidFrom, DateOnly? ValidTo);
