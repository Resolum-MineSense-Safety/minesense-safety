using System.Security.Cryptography;
using MineSenseSafety.Shared.Domain.Model;
using ReportingService.Application.Internal.CommandServices;
using ReportingService.Domain.Model.Aggregates;
using ReportingService.Domain.Model.ValueObjects;
using ReportingService.Domain.Repositories;
using ReportingService.Domain.Services;

namespace ReportingService.Application.Internal.Serverless;

/// <summary>
/// Finishes the PDF jobs delegated to the Lambda: Ready when the object is in storage, Failed when it
/// does not appear in time (the Lambda discarded the message or it ended in the dead-letter queue).
/// Safe with several replicas: the artifact is unique per job and a repeated check changes nothing.
/// </summary>
public class ReportCompletionService(
    IReportJobRepository jobRepository,
    IReportArtifactRepository artifactRepository,
    IReportObjectStore objectStore,
    ServerlessReportingOptions options,
    TimeProvider timeProvider,
    ILogger<ReportCompletionService> logger)
{
    public async Task CheckPendingJobsAsync(CancellationToken cancellationToken)
    {
        var jobs = await jobRepository.FindByStatusAsync(ReportJobStatus.Rendering);

        // CSV jobs are rendered in process and are only in this status for the duration of their request.
        foreach (var job in jobs.Where(j => j.Format == ReportFormat.Pdf))
        {
            try
            {
                await CheckJobAsync(job, cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogError(exception, "Report job {ReportJobId} could not be checked.", job.Id);
            }
        }
    }

    private async Task CheckJobAsync(ReportJob job, CancellationToken cancellationToken)
    {
        // An artifact without a Ready job means an earlier check stopped halfway: only the status is missing.
        if (await artifactRepository.FindByReportJobIdAsync(job.Id) is null)
        {
            var key = ReportObjectKey.For(job.Id, job.RequestedAt);
            await using var content = await TryOpenAsync(job, key, cancellationToken);
            if (content is null)
            {
                await FailIfTimedOutAsync(job);
                return;
            }

            await StoreArtifactAsync(job, key, content, cancellationToken);
        }

        job.MarkReady(timeProvider.GetUtcNow());
        await jobRepository.UpdateAsync(job);
    }

    /// <summary>A storage error counts as "not there yet", so it cannot keep the job pending forever.</summary>
    private async Task<Stream?> TryOpenAsync(ReportJob job, string key, CancellationToken cancellationToken)
    {
        try
        {
            return await objectStore.OpenReadAsync(key, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Report object {Key} of job {ReportJobId} could not be read.", key, job.Id);
            return null;
        }
    }

    private async Task StoreArtifactAsync(ReportJob job, string key, Stream content, CancellationToken cancellationToken)
    {
        var sha256 = await SHA256.HashDataAsync(content, cancellationToken);
        var artifact = ReportArtifact.Stored(job.Id, $"/api/v1/reports/{job.Id}/download",
            $"{job.DefinitionCode}-{job.Id:N}.pdf", "application/pdf", key, sha256,
            timeProvider.GetUtcNow() + ReportingCommandService.ArtifactLifetime);

        try
        {
            await artifactRepository.AddAsync(artifact);
        }
        catch (DomainConflictException)
        {
            logger.LogDebug("Another replica already stored the artifact of job {ReportJobId}.", job.Id);
        }
    }

    private async Task FailIfTimedOutAsync(ReportJob job)
    {
        var now = timeProvider.GetUtcNow();
        if (now - job.RequestedAt < options.GenerationTimeout) return;

        logger.LogWarning("Report job {ReportJobId} timed out; check the report-requests dead-letter queue.", job.Id);
        job.MarkFailed($"The report was not generated within {options.GenerationTimeout.TotalMinutes:0} minutes.", now);
        await jobRepository.UpdateAsync(job);
    }
}
