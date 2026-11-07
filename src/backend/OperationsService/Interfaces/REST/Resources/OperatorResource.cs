namespace OperationsService.Interfaces.REST.Resources;

public record OperatorResource(
    Guid Id, string EmployeeCode, string FirstName, string LastName, string FullName, Guid MiningUnitId, string Status);
