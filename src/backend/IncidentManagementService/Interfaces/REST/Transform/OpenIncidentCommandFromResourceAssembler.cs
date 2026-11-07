using IncidentManagementService.Domain.Model.Commands;
using IncidentManagementService.Domain.Model.ValueObjects;
using IncidentManagementService.Interfaces.REST.Resources;
using MineSenseSafety.Shared.Domain.Model;

namespace IncidentManagementService.Interfaces.REST.Transform;

public static class OpenIncidentCommandFromResourceAssembler
{
    public static OpenIncidentCommand ToCommandFromResource(OpenIncidentResource resource)
    {
        if (!Enum.TryParse<IncidentSeverity>(resource.Severity, ignoreCase: true, out var severity))
            throw new DomainException($"Unknown incident severity '{resource.Severity}'.");

        return new OpenIncidentCommand(resource.OperatorId, resource.AlertId, severity);
    }
}
