using ReportingService.Domain.Model.Aggregates;
using ReportingService.Domain.Model.Queries;
using ReportingService.Domain.Services;

namespace ReportingService.Application.Internal.QueryServices;

public class ReportingQueryService : IReportingQueryService
{
    public Task<IEnumerable<ReportDefinition>> Handle(GetReportDefinitionsQuery query) =>
        throw new NotImplementedException("Fase 1 (US22): definitions query is not implemented yet.");

    public Task<ReportJob?> Handle(GetReportJobByIdQuery query) =>
        throw new NotImplementedException("Fase 2 (US22): report job query is not implemented yet.");

    public Task<IEnumerable<ReportJob>> Handle(GetReportJobsQuery query) =>
        throw new NotImplementedException("Fase 2 (US22): report jobs query is not implemented yet.");
}
