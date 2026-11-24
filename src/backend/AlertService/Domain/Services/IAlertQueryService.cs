using AlertService.Domain.Model.Aggregates;
using AlertService.Domain.Model.Queries;

namespace AlertService.Domain.Services;

public interface IAlertQueryService
{
    Task<Alert?> Handle(GetAlertByIdQuery query);
    Task<IEnumerable<Alert>> Handle(GetAlertsByOperatorIdQuery query);
    Task<IEnumerable<Alert>> Handle(GetAlertsByStatusQuery query);
}
