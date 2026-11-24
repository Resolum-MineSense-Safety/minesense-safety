using PrivacyAuditService.Domain.Model.Aggregates;
using PrivacyAuditService.Domain.Repositories;

namespace PrivacyAuditService.Infrastructure.Persistence.InMemory;

/// <summary>
/// Thread-safe in-memory repository used while the MongoDB adapter (ADR-005) is implemented.
/// </summary>
public class InMemoryProcessingConsentRepository
    : MineSenseSafety.Shared.Infrastructure.Persistence.InMemory.InMemoryRepository<ProcessingConsent>,
      IProcessingConsentRepository
{
    public Task<IEnumerable<ProcessingConsent>> FindByOperatorIdAsync(Guid operatorId) =>
        Task.FromResult<IEnumerable<ProcessingConsent>>(
            Store.Values.Where(consent => consent.OperatorId == operatorId)
                .OrderByDescending(consent => consent.DecidedAt)
                .ToList());
}
