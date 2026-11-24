using PrivacyAuditService.Domain.Model.ValueObjects;

namespace PrivacyAuditService.Domain.Model.Commands;

public record RegisterConsentDecisionCommand(
    Guid OperatorId,
    string PolicyVersion,
    IEnumerable<string> Purposes,
    ConsentStatus Status);
