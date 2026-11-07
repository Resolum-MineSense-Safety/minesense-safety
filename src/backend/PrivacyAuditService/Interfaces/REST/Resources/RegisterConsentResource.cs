using System.Text.Json.Serialization;

namespace PrivacyAuditService.Interfaces.REST.Resources;

public record RegisterConsentResource(
    [property: JsonRequired] Guid OperatorId,
    [property: JsonRequired] string PolicyVersion,
    [property: JsonRequired] IEnumerable<string> Purposes,
    [property: JsonRequired] string Status);
