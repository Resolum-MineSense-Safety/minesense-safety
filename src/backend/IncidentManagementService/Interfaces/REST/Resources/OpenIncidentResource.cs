using System.Text.Json.Serialization;

namespace IncidentManagementService.Interfaces.REST.Resources;

public record OpenIncidentResource(
    [property: JsonRequired] Guid OperatorId,
    [property: JsonRequired] Guid AlertId,
    string Severity
);
