namespace PrivacyAuditService.Interfaces.REST.Resources;

public record ProcessingPurposeResource(string Code, string Description, bool IsMandatory);

public record ProcessingPurposesResource(string PolicyVersion, IEnumerable<ProcessingPurposeResource> Purposes);
