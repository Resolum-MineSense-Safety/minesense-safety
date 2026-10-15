using PrivacyAuditService.Domain.Model.Aggregates;
using PrivacyAuditService.Domain.Repositories;

namespace PrivacyAuditService.Infrastructure.Persistence.InMemory;

/// <summary>
/// Thread-safe in-memory repository used while the MongoDB adapter (ADR-005) is implemented.
/// Append-only by contract: update and delete are never invoked for audit entries.
/// </summary>
public class InMemoryAccessAuditRepository
    : MineSenseSafety.Shared.Infrastructure.Persistence.InMemory.InMemoryRepository<AccessAuditEntry>,
      IAccessAuditRepository
{
    public Task<IEnumerable<AccessAuditEntry>> FindByResourceAsync(string resourceType, Guid resourceId) =>
        Task.FromResult<IEnumerable<AccessAuditEntry>>(
            Store.Values.Where(entry => entry.ResourceType == resourceType && entry.ResourceId == resourceId)
                .OrderBy(entry => entry.AccessedAt)
                .ToList());
}
