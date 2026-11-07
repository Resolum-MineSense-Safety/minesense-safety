using ReportingService.Domain.Model.Aggregates;
using ReportingService.Domain.Model.Queries;

namespace ReportingService.Domain.Services;

public interface IReportingQueryService
{
    Task<IEnumerable<ReportDefinition>> Handle(GetReportDefinitionsQuery query);
    Task<ReportJob?> Handle(GetReportJobByIdQuery query);
    Task<IEnumerable<ReportJob>> Handle(GetReportJobsQuery query);
    Task<ReportArtifact?> FindArtifactByJobIdAsync(Guid reportJobId);
}
