using PrivacyAuditService.Domain.Model.Aggregates;
using PrivacyAuditService.Domain.Model.Commands;
using PrivacyAuditService.Domain.Repositories;
using PrivacyAuditService.Domain.Services;

namespace PrivacyAuditService.Application.Internal.CommandServices;

public class PrivacyAuditCommandService(
    IProcessingConsentRepository consentRepository,
    IAccessAuditRepository accessAuditRepository,
    IComplianceReportRepository reportRepository,
    TimeProvider timeProvider) : IPrivacyAuditCommandService
{
    public Task<ProcessingConsent> Handle(RegisterConsentDecisionCommand command)
    {
        _ = consentRepository;
        _ = accessAuditRepository;
        _ = reportRepository;
        _ = timeProvider;
        throw new NotImplementedException("Fase 1 (US20): consent decision handling is not implemented yet.");
    }

    public Task<AccessAuditEntry> Handle(RegisterAccessAttemptCommand command) =>
        throw new NotImplementedException("Fase 1 (US21): access attempt handling is not implemented yet.");

    public Task<ComplianceReport> Handle(RequestSafetyReportCommand command) =>
        throw new NotImplementedException("Fase 2 (US22): report request handling is not implemented yet.");
}
