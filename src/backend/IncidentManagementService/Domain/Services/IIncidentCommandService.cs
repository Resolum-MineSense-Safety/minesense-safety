using IncidentManagementService.Domain.Model.Aggregates;
using IncidentManagementService.Domain.Model.Commands;
using IncidentManagementService.Domain.Model.Entities;

namespace IncidentManagementService.Domain.Services;

public interface IIncidentCommandService
{
    Task<Incident> Handle(OpenIncidentCommand command);
    Task<Incident?> Handle(AssignIncidentCommand command);
    Task<IncidentAction?> Handle(RecordIncidentActionCommand command);
    Task<Incident?> Handle(CloseIncidentCommand command);

    /// <summary>Returns the incidents that were escalated.</summary>
    Task<IEnumerable<Incident>> Handle(EscalateOverdueIncidentsCommand command);
}
