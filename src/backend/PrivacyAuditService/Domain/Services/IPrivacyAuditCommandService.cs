using PrivacyAuditService.Domain.Model.Aggregates;
using PrivacyAuditService.Domain.Model.Commands;

namespace PrivacyAuditService.Domain.Services;

public interface IPrivacyAuditCommandService
{
    /// <returns>The decision and whether it was newly created (false when idempotent replay).</returns>
    Task<(ProcessingConsent Consent, bool Created)> Handle(RegisterConsentDecisionCommand command);
    Task<AccessAuditEntry> Handle(RegisterAccessAttemptCommand command);
    Task<ComplianceReport> Handle(RequestSafetyReportCommand command);
}
