using IncidentManagementService.Domain.Model.Aggregates;
using IncidentManagementService.Domain.Services;
using IncidentManagementService.Interfaces.REST.Resources;

namespace IncidentManagementService.Interfaces.REST.Transform;

public static class IncidentEscalationResourceFromEntityAssembler
{
    public static IncidentEscalationResource ToResourceFromEntity(Incident incident)
    {
        var status = !incident.IsEscalationApplicable ? "NotApplicable"
            : incident.EscalationLevel == 0 ? "NotRequired"
            : "Escalated";

        return new IncidentEscalationResource(incident.Id, status, incident.EscalationLevel,
            IncidentEscalationPolicy.NotifiedRolesAt(incident.EscalationLevel), incident.LastEscalatedAt);
    }
}
