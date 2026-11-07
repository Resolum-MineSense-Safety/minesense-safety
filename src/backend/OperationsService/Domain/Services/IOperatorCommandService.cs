using OperationsService.Domain.Model.Aggregates;
using OperationsService.Domain.Model.Commands;

namespace OperationsService.Domain.Services;

public interface IOperatorCommandService
{
    Task<Operator> Handle(RegisterOperatorCommand command);
    Task<Operator?> Handle(UpdateOperatorCommand command);
}
