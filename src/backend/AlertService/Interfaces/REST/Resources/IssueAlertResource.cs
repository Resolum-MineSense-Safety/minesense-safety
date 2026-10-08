using System.Text.Json.Serialization;

namespace AlertService.Interfaces.REST.Resources;

public record IssueAlertResource(
    [property: JsonRequired] Guid OperatorId,
    [property: JsonRequired] Guid AssessmentId,
    string Severity
);
