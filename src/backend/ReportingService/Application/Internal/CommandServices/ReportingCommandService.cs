using ReportingService.Domain.Model.Aggregates;
using ReportingService.Domain.Model.Commands;
using ReportingService.Domain.Repositories;
using ReportingService.Domain.Services;
using MineSenseSafety.Shared.Domain.Model;

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

    public async Task<ReportJob> Handle(RequestReportGenerationCommand command)
    {
        _ = artifactRepository;

        var definition = await definitionRepository.FindByCodeAsync(command.DefinitionCode);
        if (definition is null)
            throw new DomainException($"Unknown report definition '{command.DefinitionCode}'.");

        var job = new ReportJob(command, timeProvider.GetUtcNow());
        await jobRepository.AddAsync(job);
        return job;
    }
}
