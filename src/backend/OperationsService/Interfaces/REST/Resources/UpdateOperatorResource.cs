namespace OperationsService.Interfaces.REST.Resources;

public record UpdateOperatorResource(string? FirstName, string? LastName, Guid MiningUnitId, string? Status);
