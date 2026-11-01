using ReportingService.Domain.Model.Aggregates;
using ReportingService.Domain.Repositories;
using ReportingService.Domain.Services;

namespace ReportingService.Infrastructure.Persistence.InMemory;

/// <summary>
/// Thread-safe in-memory repository used while the MongoDB adapter (ADR-005) is implemented.
/// </summary>
public class InMemoryReportDefinitionRepository
    : MineSenseSafety.Shared.Infrastructure.Persistence.InMemory.InMemoryRepository<ReportDefinition>,
      IReportDefinitionRepository
{
    public Task<ReportDefinition?> FindByCodeAsync(string code) =>
        Task.FromResult(Store.Values
            .Where(d => string.Equals(d.Code, code, StringComparison.OrdinalIgnoreCase))
            .MaxBy(d => d.Version, ReportDefinitionVersionComparer.Instance));

    public Task<ReportDefinition?> FindByCodeAndVersionAsync(string code, string version) =>
        Task.FromResult(Store.Values.FirstOrDefault(d =>
            string.Equals(d.Code, code, StringComparison.OrdinalIgnoreCase)
            && string.Equals(d.Version, version, StringComparison.OrdinalIgnoreCase)));
}
