using MineSenseSafety.Shared.Domain.Repositories;
using ReportingService.Domain.Model.Aggregates;

namespace ReportingService.Domain.Repositories;

public interface IReportDefinitionRepository : IBaseRepository<ReportDefinition>
{
    /// <summary>Latest version of the indicator (see <c>ReportDefinitionVersionComparer</c>).</summary>
    Task<ReportDefinition?> FindByCodeAsync(string code);

    Task<ReportDefinition?> FindByCodeAndVersionAsync(string code, string version);
}
