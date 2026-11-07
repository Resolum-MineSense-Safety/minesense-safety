using MineSenseSafety.Shared.Domain.Repositories;
using ReportingService.Domain.Model.Aggregates;

namespace ReportingService.Domain.Repositories;

public interface IReportArtifactRepository : IBaseRepository<ReportArtifact>
{
    Task<ReportArtifact?> FindByReportJobIdAsync(Guid reportJobId);
}
