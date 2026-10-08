using IncidentManagementService.Domain.Model.Aggregates;
using IncidentManagementService.Domain.Model.Commands;

namespace IncidentManagementService.Domain.Services;

public interface IIncidentCommandService
{
    Task<Incident> Handle(OpenIncidentCommand command);
    Task<Incident?> Handle(AssignIncidentCommand command);
    Task<Incident?> Handle(RegisterIncidentActionCommand command);
    Task<Incident?> Handle(EscalateIncidentCommand command);
    Task<Incident?> Handle(CloseIncidentCommand command);
}
