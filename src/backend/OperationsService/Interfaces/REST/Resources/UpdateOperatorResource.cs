using System.Text.Json.Serialization;

namespace OperationsService.Interfaces.REST.Resources;

public record UpdateOperatorResource(
    string? FirstName,
    string? LastName,
    [property: JsonRequired] Guid MiningUnitId,
    string? Status
);