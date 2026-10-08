using System.Text.Json.Serialization;

namespace OperationsService.Interfaces.REST.Resources;

public record UpdateVehicleResource(string? Model, string? Status, [property: JsonRequired] Guid FleetId);
