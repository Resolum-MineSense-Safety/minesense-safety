using IncidentManagementService.Domain.Model.Aggregates;
using IncidentManagementService.Domain.Model.Queries;

namespace IncidentManagementService.Domain.Services;

public interface IIncidentQueryService
{
    Task<Incident?> Handle(GetIncidentByIdQuery query);
    Task<IEnumerable<Incident>> Handle(GetIncidentsByStatusQuery query);
    Task<IEnumerable<Incident>> Handle(GetIncidentsByOperatorIdQuery query);
}
