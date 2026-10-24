using OperationsService.Domain.Model.Aggregates;
using OperationsService.Interfaces.REST.Resources;

namespace OperationsService.Interfaces.REST.Transform;

public static class OperatorResourceFromEntityAssembler
{
    public static OperatorResource ToResourceFromEntity(Operator @operator) =>
        new(@operator.Id, @operator.EmployeeCode, @operator.FirstName, @operator.LastName, @operator.FullName,
            @operator.MiningUnitId, @operator.Status.ToString());
}
