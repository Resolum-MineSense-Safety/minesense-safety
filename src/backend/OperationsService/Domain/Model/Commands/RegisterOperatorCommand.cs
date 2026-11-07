namespace OperationsService.Domain.Model.Commands;

public record RegisterOperatorCommand(string EmployeeCode, string FirstName, string LastName, Guid MiningUnitId);
