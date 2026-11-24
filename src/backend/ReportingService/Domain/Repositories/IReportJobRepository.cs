using MineSenseSafety.Shared.Domain.Repositories;
using ReportingService.Domain.Model.Aggregates;
using ReportingService.Domain.Model.ValueObjects;

namespace ReportingService.Domain.Repositories;

public interface IReportJobRepository : IBaseRepository<ReportJob>
{
    Task<IEnumerable<ReportJob>> FindByStatusAsync(ReportJobStatus status);
}
