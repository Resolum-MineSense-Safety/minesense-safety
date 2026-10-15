using PrivacyAuditService.Domain.Model.Aggregates;
using PrivacyAuditService.Domain.Model.Queries;
using PrivacyAuditService.Domain.Repositories;
using PrivacyAuditService.Domain.Services;

namespace PrivacyAuditService.Application.Internal.QueryServices;

public class PrivacyAuditQueryService(
    IProcessingConsentRepository consentRepository,
    IAccessAuditRepository accessAuditRepository)
    : IPrivacyAuditQueryService
{
    public async Task<ProcessingConsent?> Handle(GetCurrentConsentByOperatorIdQuery query) =>
        (await consentRepository.FindByOperatorIdAsync(query.OperatorId)).FirstOrDefault();

    public Task<IEnumerable<ProcessingConsent>> Handle(GetConsentHistoryByOperatorIdQuery query) =>
        consentRepository.FindByOperatorIdAsync(query.OperatorId);

    public async Task<IEnumerable<AccessAuditEntry>> Handle(GetAccessLogQuery query)
    {
        var entries = await accessAuditRepository.ListAsync();

        if (query.OperatorId.HasValue)
            entries = entries.Where(e => e.ResourceId == query.OperatorId.Value);
        if (query.UserId.HasValue)
            entries = entries.Where(e => e.UserId == query.UserId.Value);
        if (!string.IsNullOrWhiteSpace(query.ResourceType))
            entries = entries.Where(e => string.Equals(e.ResourceType, query.ResourceType, StringComparison.OrdinalIgnoreCase));
        if (query.From.HasValue)
            entries = entries.Where(e => e.AccessedAt >= query.From.Value);
        if (query.To.HasValue)
            entries = entries.Where(e => e.AccessedAt <= query.To.Value);
        if (query.Decision.HasValue)
            entries = entries.Where(e => e.Decision == query.Decision.Value);

        return entries
            .OrderByDescending(e => e.AccessedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToList();
    }

    public Task<ComplianceReport?> Handle(GetReportByIdQuery query) =>
        throw new NotImplementedException("Fase 2 (US22): report query is not implemented yet.");
}
