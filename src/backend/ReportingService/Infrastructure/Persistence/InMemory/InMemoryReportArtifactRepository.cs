using ReportingService.Domain.Model.Aggregates;
using ReportingService.Domain.Repositories;

namespace ReportingService.Infrastructure.Persistence.InMemory;

/// <summary>
/// Thread-safe in-memory repository used while the MongoDB adapter (ADR-005) is implemented.
/// </summary>
public class InMemoryReportArtifactRepository
    : MineSenseSafety.Shared.Infrastructure.Persistence.InMemory.InMemoryRepository<ReportArtifact>,
      IReportArtifactRepository
{
    public Task<ReportArtifact?> FindByReportJobIdAsync(Guid reportJobId) =>
        Task.FromResult(Store.Values.FirstOrDefault(a => a.ReportJobId == reportJobId));
}
