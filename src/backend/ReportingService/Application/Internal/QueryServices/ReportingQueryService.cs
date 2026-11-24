using ReportingService.Domain.Model.Aggregates;
using ReportingService.Domain.Model.Queries;
using ReportingService.Domain.Repositories;
using ReportingService.Domain.Services;

namespace ReportingService.Application.Internal.QueryServices;

public class ReportingQueryService(
    IReportDefinitionRepository definitionRepository,
    IReportJobRepository jobRepository,
    IReportArtifactRepository artifactRepository) : IReportingQueryService
{
    public async Task<IEnumerable<ReportDefinition>> Handle(GetReportDefinitionsQuery query)
    {
        var definitions = await definitionRepository.ListAsync();
        return definitions
            .Where(d => !query.OnlyActive || d.IsActive)
            .OrderBy(d => d.Code, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public Task<ReportJob?> Handle(GetReportJobByIdQuery query) =>
        jobRepository.FindByIdAsync(query.ReportJobId);

    public async Task<IEnumerable<ReportJob>> Handle(GetReportJobsQuery query)
    {
        var jobs = await jobRepository.ListAsync();

        if (!string.IsNullOrWhiteSpace(query.SiteId))
            jobs = jobs.Where(j => string.Equals(j.SiteId, query.SiteId, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(query.FleetId))
            jobs = jobs.Where(j => string.Equals(j.FleetId, query.FleetId, StringComparison.OrdinalIgnoreCase));
        if (query.From.HasValue)
            jobs = jobs.Where(j => j.RequestedAt >= query.From.Value);
        if (query.To.HasValue)
            jobs = jobs.Where(j => j.RequestedAt <= query.To.Value);

        return jobs
            .OrderByDescending(j => j.RequestedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToList();
    }

    public Task<ReportArtifact?> FindArtifactByJobIdAsync(Guid reportJobId) =>
        artifactRepository.FindByReportJobIdAsync(reportJobId);
}
