using OperationsService.Domain.Model.Aggregates;
using OperationsService.Domain.Model.Commands;

namespace OperationsService.Domain.Services;

public interface IFleetCommandService
{
    Task<Fleet> Handle(RegisterFleetCommand command);
}
