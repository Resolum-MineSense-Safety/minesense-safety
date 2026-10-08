using System.Text.Json.Serialization;

namespace OperationsService.Interfaces.REST.Resources;

public record RegisterVehicleResource(string? Code, string? Model, [property: JsonRequired] Guid FleetId);
