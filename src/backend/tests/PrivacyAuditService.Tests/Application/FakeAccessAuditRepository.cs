using PrivacyAuditService.Domain.Model.Aggregates;
using PrivacyAuditService.Domain.Repositories;

namespace PrivacyAuditService.Tests.Application;

public class FakeAccessAuditRepository : IAccessAuditRepository
{
    private readonly List<AccessAuditEntry> store = new();

    public Task AddAsync(AccessAuditEntry aggregate)
    {
        store.Add(aggregate);
        return Task.CompletedTask;
    }

    public Task<AccessAuditEntry?> FindByIdAsync(Guid id) =>
        Task.FromResult(store.FirstOrDefault(e => e.Id == id));

    public Task<IEnumerable<AccessAuditEntry>> ListAsync() =>
        Task.FromResult<IEnumerable<AccessAuditEntry>>(store.ToList());

    public Task UpdateAsync(AccessAuditEntry aggregate) => Task.CompletedTask;

    public Task<IEnumerable<AccessAuditEntry>> FindByResourceAsync(string resourceType, Guid resourceId) =>
        Task.FromResult<IEnumerable<AccessAuditEntry>>(
            store.Where(e => e.ResourceType == resourceType && e.ResourceId == resourceId)
                .OrderBy(e => e.AccessedAt)
                .ToList());
}
