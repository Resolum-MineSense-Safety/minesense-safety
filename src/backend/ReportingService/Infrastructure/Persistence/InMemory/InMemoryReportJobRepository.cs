using ReportingService.Domain.Model.Aggregates;
using ReportingService.Domain.Model.ValueObjects;
using ReportingService.Domain.Repositories;

namespace ReportingService.Infrastructure.Persistence.InMemory;

/// <summary>
/// Thread-safe in-memory repository used while the MongoDB adapter (ADR-005) is implemented.
/// </summary>
public class InMemoryReportJobRepository
    : MineSenseSafety.Shared.Infrastructure.Persistence.InMemory.InMemoryRepository<ReportJob>,
      IReportJobRepository
{
    public Task<IEnumerable<ReportJob>> FindByStatusAsync(ReportJobStatus status) =>
        Task.FromResult<IEnumerable<ReportJob>>(Store.Values.Where(j => j.Status == status).ToList());
}
