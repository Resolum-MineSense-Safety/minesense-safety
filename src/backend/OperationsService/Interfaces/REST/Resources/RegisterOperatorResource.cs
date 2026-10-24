namespace OperationsService.Interfaces.REST.Resources;

public record RegisterOperatorResource(string? EmployeeCode, string? FirstName, string? LastName, Guid MiningUnitId);
