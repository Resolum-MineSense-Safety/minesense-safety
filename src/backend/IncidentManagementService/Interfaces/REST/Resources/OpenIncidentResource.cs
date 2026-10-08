using System.Text.Json.Serialization;

namespace IncidentManagementService.Interfaces.REST.Resources;

public record OpenIncidentResource(
    [property: JsonRequired] Guid AlertId,
    [property: JsonRequired] Guid OperatorId
);
