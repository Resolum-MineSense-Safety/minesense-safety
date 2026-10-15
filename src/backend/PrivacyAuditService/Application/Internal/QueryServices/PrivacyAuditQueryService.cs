using PrivacyAuditService.Domain.Model.Aggregates;
using PrivacyAuditService.Domain.Model.Queries;
using PrivacyAuditService.Domain.Services;

namespace PrivacyAuditService.Application.Internal.QueryServices;

public class PrivacyAuditQueryService : IPrivacyAuditQueryService
{
    public Task<ProcessingConsent?> Handle(GetCurrentConsentByOperatorIdQuery query) =>
        throw new NotImplementedException("Fase 1 (US20): current consent query is not implemented yet.");

    public Task<IEnumerable<ProcessingConsent>> Handle(GetConsentHistoryByOperatorIdQuery query) =>
        throw new NotImplementedException("Fase 1 (US20): consent history query is not implemented yet.");

    public Task<IEnumerable<AccessAuditEntry>> Handle(GetAccessLogQuery query) =>
        throw new NotImplementedException("Fase 1 (US21): access log query is not implemented yet.");

    public Task<ComplianceReport?> Handle(GetReportByIdQuery query) =>
        throw new NotImplementedException("Fase 2 (US22): report query is not implemented yet.");
}
