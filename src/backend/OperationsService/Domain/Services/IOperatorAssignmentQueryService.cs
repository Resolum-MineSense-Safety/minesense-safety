using OperationsService.Domain.Model.Aggregates;
using OperationsService.Domain.Model.Queries;
using OperationsService.Domain.Model.ValueObjects;

namespace OperationsService.Domain.Services;

public interface IOperatorAssignmentQueryService
{
    Task<OperatorAssignment?> Handle(GetAssignmentByIdQuery query);
    Task<OperatorAssignment?> Handle(GetActiveAssignmentByOperatorQuery query);
    Task<IEnumerable<OperatorAssignment>> Handle(GetAssignmentHistoryByOperatorQuery query);
    Task<OperationalContextValidation> Handle(ValidateOperationalContextQuery query);
}
