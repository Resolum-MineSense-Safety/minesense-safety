using PrivacyAuditService.Domain.Model.Aggregates;
using PrivacyAuditService.Domain.Model.Queries;
using PrivacyAuditService.Domain.Repositories;
using PrivacyAuditService.Domain.Services;

namespace PrivacyAuditService.Application.Internal.QueryServices;

public class PrivacyAuditQueryService(IProcessingConsentRepository consentRepository)
    : IPrivacyAuditQueryService
{
    public async Task<ProcessingConsent?> Handle(GetCurrentConsentByOperatorIdQuery query) =>
        (await consentRepository.FindByOperatorIdAsync(query.OperatorId)).FirstOrDefault();

    public Task<IEnumerable<ProcessingConsent>> Handle(GetConsentHistoryByOperatorIdQuery query) =>
        consentRepository.FindByOperatorIdAsync(query.OperatorId);

    public Task<IEnumerable<AccessAuditEntry>> Handle(GetAccessLogQuery query) =>
        throw new NotImplementedException("Fase 1 (US21): access log query is not implemented yet.");

    public Task<ComplianceReport?> Handle(GetReportByIdQuery query) =>
        throw new NotImplementedException("Fase 2 (US22): report query is not implemented yet.");
}
