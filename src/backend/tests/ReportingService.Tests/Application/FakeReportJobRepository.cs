using ReportingService.Domain.Model.Aggregates;
using ReportingService.Domain.Repositories;

namespace ReportingService.Tests.Application;

public class FakeReportJobRepository : IReportJobRepository
{
    private readonly List<ReportJob> store = new();

    public Task AddAsync(ReportJob aggregate)
    {
        store.Add(aggregate);
        return Task.CompletedTask;
    }

    public Task<ReportJob?> FindByIdAsync(Guid id) =>
        Task.FromResult(store.FirstOrDefault(j => j.Id == id));

    public Task<IEnumerable<ReportJob>> ListAsync() =>
        Task.FromResult<IEnumerable<ReportJob>>(store.ToList());

    public Task UpdateAsync(ReportJob aggregate) => Task.CompletedTask;
}
