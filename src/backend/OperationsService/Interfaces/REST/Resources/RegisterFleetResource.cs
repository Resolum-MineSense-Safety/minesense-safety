using System.Text.Json.Serialization;

namespace OperationsService.Interfaces.REST.Resources;

public record RegisterFleetResource([property: JsonRequired] Guid MiningUnitId, string? Name);
