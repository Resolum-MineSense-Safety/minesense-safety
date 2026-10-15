using ReportingService.Domain.Model.Aggregates;
using ReportingService.Domain.Model.Commands;
using ReportingService.Domain.Repositories;
using ReportingService.Domain.Services;

namespace ReportingService.Application.Internal.CommandServices;

public class ReportingCommandService(
    IReportDefinitionRepository definitionRepository,
    IReportJobRepository jobRepository,
    IReportArtifactRepository artifactRepository,
    TimeProvider timeProvider) : IReportingCommandService
{
    public Task<ReportDefinition> Handle(DefineReportIndicatorCommand command)
    {
        _ = definitionRepository;
        _ = jobRepository;
        _ = artifactRepository;
        _ = timeProvider;
        throw new NotImplementedException("Fase 1 (US22): indicator definition handling is not implemented yet.");
    }

    public Task<ReportJob> Handle(RequestReportGenerationCommand command) =>
        throw new NotImplementedException("Fase 2 (US22): report generation handling is not implemented yet.");
}
