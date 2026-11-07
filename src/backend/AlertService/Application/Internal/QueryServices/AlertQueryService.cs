using AlertService.Domain.Model.Aggregates;
using AlertService.Domain.Model.Queries;
using AlertService.Domain.Repositories;
using AlertService.Domain.Services;

namespace AlertService.Application.Internal.QueryServices;

public class AlertQueryService(IAlertRepository alertRepository) : IAlertQueryService
{
    public Task<Alert?> Handle(GetAlertByIdQuery query) =>
        alertRepository.FindByIdAsync(query.AlertId);

    public Task<IEnumerable<Alert>> Handle(GetAlertsByOperatorIdQuery query) =>
        alertRepository.FindByOperatorIdAsync(query.OperatorId);

    public Task<IEnumerable<Alert>> Handle(GetAlertsByStatusQuery query) =>
        alertRepository.FindByStatusAsync(query.Status);
}
