using ReportingService.Domain.Model.Aggregates;
using ReportingService.Domain.Model.Commands;

namespace ReportingService.Domain.Services;

public interface IReportingCommandService
{
    Task<ReportDefinition> Handle(DefineReportIndicatorCommand command);
    Task<ReportJob> Handle(RequestReportGenerationCommand command);
}
