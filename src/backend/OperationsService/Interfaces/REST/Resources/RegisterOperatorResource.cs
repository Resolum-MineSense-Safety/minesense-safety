using System.Text.Json.Serialization;

namespace OperationsService.Interfaces.REST.Resources;

public record RegisterOperatorResource(
    string? EmployeeCode,
    string? FirstName,
    string? LastName,
    [property: JsonRequired] Guid MiningUnitId
);