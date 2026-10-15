using PrivacyAuditService.Domain.Model.Aggregates;
using PrivacyAuditService.Domain.Model.Queries;

namespace PrivacyAuditService.Domain.Services;

public interface IPrivacyAuditQueryService
{
    Task<ProcessingConsent?> Handle(GetCurrentConsentByOperatorIdQuery query);
    Task<IEnumerable<ProcessingConsent>> Handle(GetConsentHistoryByOperatorIdQuery query);
    Task<IEnumerable<AccessAuditEntry>> Handle(GetAccessLogQuery query);
    Task<ComplianceReport?> Handle(GetReportByIdQuery query);
}
