using IncidentManagementService.Domain.Model.Aggregates;
using IncidentManagementService.Domain.Model.Commands;
using IncidentManagementService.Domain.Model.ValueObjects;
using IncidentManagementService.Domain.Repositories;
using IncidentManagementService.Domain.Services;

namespace IncidentManagementService.Application.Internal.CommandServices;

public class IncidentCommandService(IIncidentRepository incidentRepository, TimeProvider timeProvider)
    : IIncidentCommandService
{
    public async Task<Incident> Handle(OpenIncidentCommand command)
    {
        var incident = new Incident(command, timeProvider.GetUtcNow());
        await incidentRepository.AddAsync(incident);
        return incident;
    }

    public Task<Incident?> Handle(AssignIncidentCommand command) =>
        Update(command.IncidentId, incident => incident.Assign(command.SupervisorId, timeProvider.GetUtcNow()));

    public Task<Incident?> Handle(RegisterIncidentActionCommand command) =>
        Update(command.IncidentId, incident => incident.RegisterAction(
            new IncidentAction(command.SupervisorId, command.Description, command.Outcome, timeProvider.GetUtcNow())));

    public Task<Incident?> Handle(EscalateIncidentCommand command) =>
        Update(command.IncidentId, incident => incident.Escalate());

    public Task<Incident?> Handle(CloseIncidentCommand command) =>
        Update(command.IncidentId, incident => incident.Close(command.Resolution, timeProvider.GetUtcNow()));

    private async Task<Incident?> Update(Guid incidentId, Action<Incident> change)
    {
        var incident = await incidentRepository.FindByIdAsync(incidentId);
        if (incident is null) return null;

        change(incident);
        await incidentRepository.UpdateAsync(incident);
        return incident;
    }
}
