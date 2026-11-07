using PrivacyAuditService.Domain.Model.Aggregates;
using PrivacyAuditService.Domain.Repositories;

namespace PrivacyAuditService.Tests.Application;

public class FakeComplianceReportRepository : IComplianceReportRepository
{
    private readonly List<ComplianceReport> store = new();

    public Task AddAsync(ComplianceReport aggregate)
    {
        store.Add(aggregate);
        return Task.CompletedTask;
    }

    public Task<ComplianceReport?> FindByIdAsync(Guid id) =>
        Task.FromResult(store.FirstOrDefault(r => r.Id == id));

    public Task<IEnumerable<ComplianceReport>> ListAsync() =>
        Task.FromResult<IEnumerable<ComplianceReport>>(store.ToList());

    public Task UpdateAsync(ComplianceReport aggregate) => Task.CompletedTask;
}
