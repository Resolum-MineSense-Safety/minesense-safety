using ReportingService.Domain.Model.Aggregates;
using ReportingService.Domain.Repositories;

namespace ReportingService.Tests.Application;

public class FakeReportDefinitionRepository : IReportDefinitionRepository
{
    private readonly List<ReportDefinition> store = new();

    public Task AddAsync(ReportDefinition aggregate)
    {
        store.Add(aggregate);
        return Task.CompletedTask;
    }

    public Task<ReportDefinition?> FindByIdAsync(Guid id) =>
        Task.FromResult(store.FirstOrDefault(d => d.Id == id));

    public Task<IEnumerable<ReportDefinition>> ListAsync() =>
        Task.FromResult<IEnumerable<ReportDefinition>>(store.ToList());

    public Task UpdateAsync(ReportDefinition aggregate) => Task.CompletedTask;

    public Task<ReportDefinition?> FindByCodeAsync(string code) =>
        Task.FromResult(store.FirstOrDefault(d =>
            string.Equals(d.Code, code, StringComparison.OrdinalIgnoreCase)));
}
