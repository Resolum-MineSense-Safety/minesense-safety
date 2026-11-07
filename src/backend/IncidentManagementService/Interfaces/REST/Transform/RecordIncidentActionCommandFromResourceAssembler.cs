using IncidentManagementService.Domain.Model.Commands;
using IncidentManagementService.Domain.Model.ValueObjects;
using IncidentManagementService.Interfaces.REST.Resources;
using MineSenseSafety.Shared.Domain.Model;

namespace IncidentManagementService.Interfaces.REST.Transform;

public static class RecordIncidentActionCommandFromResourceAssembler
{
    public static RecordIncidentActionCommand ToCommandFromResource(Guid incidentId,
        RecordIncidentActionResource resource)
    {
        if (!Enum.TryParse<IncidentActionType>(resource.ActionType, ignoreCase: true, out var actionType))
            throw new DomainException($"Unknown incident action type '{resource.ActionType}'.");

        return new RecordIncidentActionCommand(incidentId, actionType, resource.Description, resource.Result,
            resource.PerformedBy, resource.PerformedAt);
    }
}
