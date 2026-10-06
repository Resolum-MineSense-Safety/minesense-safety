using IncidentManagementService.Domain.Model.Aggregates;
using IncidentManagementService.Domain.Model.Queries;
using IncidentManagementService.Domain.Repositories;
using IncidentManagementService.Domain.Services;

namespace IncidentManagementService.Application.Internal.QueryServices;

public class IncidentQueryService(IIncidentRepository incidentRepository) : IIncidentQueryService
{
    public Task<Incident?> Handle(GetIncidentByIdQuery query) =>
        incidentRepository.FindByIdAsync(query.IncidentId);

    public Task<IEnumerable<Incident>> Handle(GetIncidentsByStatusQuery query) =>
        incidentRepository.FindByStatusAsync(query.Status);

    public Task<IEnumerable<Incident>> Handle(GetIncidentsByOperatorIdQuery query) =>
        incidentRepository.FindByOperatorIdAsync(query.OperatorId);
}
