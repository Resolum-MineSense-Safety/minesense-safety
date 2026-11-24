using OperationsService.Domain.Model.Commands;
using OperationsService.Interfaces.REST.Resources;

namespace OperationsService.Interfaces.REST.Transform;

public static class RegisterOperatorCommandFromResourceAssembler
{
    public static RegisterOperatorCommand ToCommandFromResource(RegisterOperatorResource resource) =>
        new(resource.EmployeeCode ?? string.Empty, resource.FirstName ?? string.Empty,
            resource.LastName ?? string.Empty, resource.MiningUnitId);
}
