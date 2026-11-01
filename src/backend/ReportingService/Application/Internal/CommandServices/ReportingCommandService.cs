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
    IReportRenderer renderer,
    TimeProvider timeProvider) : IReportingCommandService
{
    /// <summary>How long a generated artifact can be downloaded.</summary>
    public static readonly TimeSpan ArtifactLifetime = TimeSpan.FromDays(7);

    public async Task<(ReportDefinition Definition, bool Created)> Handle(DefineReportIndicatorCommand command)
    {
        var existing = await definitionRepository.FindByCodeAndVersionAsync(command.Code, command.Version);
        if (existing is not null)
            return (existing, false);

        var definition = new ReportDefinition(command);
        await definitionRepository.AddAsync(definition);
        return (definition, true);
    }

    public async Task<ReportJob> Handle(RequestReportGenerationCommand command)
    {
        var definition = await definitionRepository.FindByCodeAsync(command.DefinitionCode);
        if (definition is null)
            throw new DomainException($"Unknown report definition '{command.DefinitionCode}'.");

        var job = new ReportJob(command, definition.Version, timeProvider.GetUtcNow());
        await jobRepository.AddAsync(job);

        job.StartRendering();
        await jobRepository.UpdateAsync(job);

        try
        {
            var generatedAt = timeProvider.GetUtcNow();
            var rendered = renderer.Render(job, definition, generatedAt);
            await artifactRepository.AddAsync(new ReportArtifact(job.Id, $"/api/v1/reports/{job.Id}/download",
                rendered.FileName, rendered.ContentType, rendered.Content, generatedAt + ArtifactLifetime));
            job.MarkReady(timeProvider.GetUtcNow());
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            job.MarkFailed(exception.Message, timeProvider.GetUtcNow());
        }

        await jobRepository.UpdateAsync(job);
        return job;
    }
}
