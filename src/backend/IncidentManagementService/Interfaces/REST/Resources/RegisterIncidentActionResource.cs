using System.Text.Json.Serialization;

namespace IncidentManagementService.Interfaces.REST.Resources;

public record RegisterIncidentActionResource(
    [property: JsonRequired] Guid SupervisorId,
    string Description,
    string Outcome
);
