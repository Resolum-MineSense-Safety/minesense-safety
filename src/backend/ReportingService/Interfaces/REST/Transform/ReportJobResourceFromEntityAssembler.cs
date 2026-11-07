using ReportingService.Domain.Model.Aggregates;
using ReportingService.Interfaces.REST.Resources;

namespace ReportingService.Interfaces.REST.Transform;

public static class ReportJobResourceFromEntityAssembler
{
    public static ReportJobResource ToResourceFromEntity(ReportJob job) =>
        new(job.Id, job.From, job.To, job.SiteId, job.FleetId, job.DefinitionCode,
            job.Format.ToString(), job.RequestedBy, job.Status.ToString(), job.RequestedAt);
}
