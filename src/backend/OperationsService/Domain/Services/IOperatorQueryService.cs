using OperationsService.Domain.Model.Aggregates;
using OperationsService.Domain.Model.Queries;

namespace OperationsService.Domain.Services;

public interface IOperatorQueryService
{
    Task<IEnumerable<Operator>> Handle(GetOperatorsQuery query);
    Task<Operator?> Handle(GetOperatorByIdQuery query);
}
