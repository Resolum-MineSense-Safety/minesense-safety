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
    public async Task<(ReportDefinition Definition, bool Created)> Handle(DefineReportIndicatorCommand command)
    {
        _ = jobRepository;
        _ = artifactRepository;
        _ = timeProvider;

        var existing = await definitionRepository.FindByCodeAsync(command.Code);
        if (existing is not null
            && string.Equals(existing.Version, command.Version, StringComparison.OrdinalIgnoreCase))
            return (existing, false);

        var definition = new ReportDefinition(command);
        await definitionRepository.AddAsync(definition);
        return (definition, true);
    }

    public Task<ReportJob> Handle(RequestReportGenerationCommand command) =>
        throw new NotImplementedException("Fase 2 (US22): report generation handling is not implemented yet.");
}
