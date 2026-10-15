using PrivacyAuditService.Domain.Model.Aggregates;
using PrivacyAuditService.Domain.Model.Commands;

namespace PrivacyAuditService.Domain.Services;

public interface IPrivacyAuditCommandService
{
    Task<ProcessingConsent> Handle(RegisterConsentDecisionCommand command);
    Task<AccessAuditEntry> Handle(RegisterAccessAttemptCommand command);
    Task<ComplianceReport> Handle(RequestSafetyReportCommand command);
}
