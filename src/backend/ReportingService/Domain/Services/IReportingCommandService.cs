using ReportingService.Domain.Model.Aggregates;
using ReportingService.Domain.Model.Commands;

namespace ReportingService.Domain.Services;

public interface IReportingCommandService
{
    /// <returns>The definition and whether it was newly created (false on exact duplicate).</returns>
    Task<(ReportDefinition Definition, bool Created)> Handle(DefineReportIndicatorCommand command);
    Task<ReportJob> Handle(RequestReportGenerationCommand command);
}
