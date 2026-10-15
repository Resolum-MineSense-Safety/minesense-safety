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
        throw new NotImplementedException("Fase 2 (US22): report job query is not implemented yet.");

    public Task<IEnumerable<ReportJob>> Handle(GetReportJobsQuery query) =>
        throw new NotImplementedException("Fase 2 (US22): report jobs query is not implemented yet.");
}
