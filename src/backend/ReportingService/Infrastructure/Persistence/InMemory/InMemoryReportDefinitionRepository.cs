using ReportingService.Domain.Model.Aggregates;
using ReportingService.Domain.Repositories;

namespace ReportingService.Infrastructure.Persistence.InMemory;

/// <summary>
/// Thread-safe in-memory repository used while the MongoDB adapter (ADR-005) is implemented.
/// </summary>
public class InMemoryReportDefinitionRepository
    : MineSenseSafety.Shared.Infrastructure.Persistence.InMemory.InMemoryRepository<ReportDefinition>,
      IReportDefinitionRepository
{
    public Task<ReportDefinition?> FindByCodeAsync(string code) =>
        Task.FromResult(Store.Values.FirstOrDefault(d =>
            string.Equals(d.Code, code, StringComparison.OrdinalIgnoreCase)));
}
