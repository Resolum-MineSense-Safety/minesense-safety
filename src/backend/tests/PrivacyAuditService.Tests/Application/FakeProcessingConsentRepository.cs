using PrivacyAuditService.Domain.Model.Aggregates;
using PrivacyAuditService.Domain.Repositories;

namespace PrivacyAuditService.Tests.Application;

public class FakeProcessingConsentRepository : IProcessingConsentRepository
{
    private readonly List<ProcessingConsent> store = new();

    public Task AddAsync(ProcessingConsent aggregate)
    {
        store.Add(aggregate);
        return Task.CompletedTask;
    }

    public Task<ProcessingConsent?> FindByIdAsync(Guid id) =>
        Task.FromResult(store.FirstOrDefault(c => c.Id == id));

    public Task<IEnumerable<ProcessingConsent>> ListAsync() =>
        Task.FromResult<IEnumerable<ProcessingConsent>>(store.ToList());

    public Task UpdateAsync(ProcessingConsent aggregate) => Task.CompletedTask;

    public Task<IEnumerable<ProcessingConsent>> FindByOperatorIdAsync(Guid operatorId) =>
        Task.FromResult<IEnumerable<ProcessingConsent>>(
            store.Where(c => c.OperatorId == operatorId)
                .OrderByDescending(c => c.DecidedAt)
                .ToList());
}
