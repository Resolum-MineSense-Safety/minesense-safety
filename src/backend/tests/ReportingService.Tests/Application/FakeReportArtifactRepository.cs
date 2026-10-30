using ReportingService.Domain.Model.Aggregates;
using ReportingService.Domain.Repositories;

namespace ReportingService.Tests.Application;

public class FakeReportArtifactRepository : IReportArtifactRepository
{
    private readonly List<ReportArtifact> store = new();

    public Task AddAsync(ReportArtifact aggregate)
    {
        store.Add(aggregate);
        return Task.CompletedTask;
    }

    public Task<ReportArtifact?> FindByIdAsync(Guid id) =>
        Task.FromResult(store.FirstOrDefault(a => a.Id == id));

    public Task<IEnumerable<ReportArtifact>> ListAsync() =>
        Task.FromResult<IEnumerable<ReportArtifact>>(store.ToList());

    public Task UpdateAsync(ReportArtifact aggregate) => Task.CompletedTask;

    public Task<ReportArtifact?> FindByReportJobIdAsync(Guid reportJobId) =>
        Task.FromResult(store.FirstOrDefault(a => a.ReportJobId == reportJobId));
}
