using System.Text.Json.Serialization;

namespace PrivacyAuditService.Interfaces.REST.Resources;

public record RegisterAccessAttemptResource(
    [property: JsonRequired] Guid UserId,
    [property: JsonRequired] string ResourceType,
    [property: JsonRequired] Guid ResourceId,
    [property: JsonRequired] IEnumerable<string> Fields,
    [property: JsonRequired] string Purpose);
