using MineSenseSafety.Shared.Domain.Repositories;
using ReportingService.Domain.Model.Aggregates;

namespace ReportingService.Domain.Repositories;

public interface IReportDefinitionRepository : IBaseRepository<ReportDefinition>
{
    Task<ReportDefinition?> FindByCodeAsync(string code);
}
