using System.Text.Json.Serialization;

namespace IncidentManagementService.Interfaces.REST.Resources;

public record AssignIncidentResource(
    [property: JsonRequired] Guid SupervisorId
);