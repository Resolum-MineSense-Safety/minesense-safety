using ReportingService.Domain.Model.Aggregates;
using ReportingService.Domain.Model.ValueObjects;
using ReportingService.Interfaces.REST.Resources;

namespace ReportingService.Interfaces.REST.Transform;

public static class ReportJobResourceFromEntityAssembler
{
    public static ReportJobResource ToResourceFromEntity(ReportJob job) =>
        new(job.Id, job.From, job.To, job.SiteId, job.FleetId, job.DefinitionCode,
            job.DefinitionVersion, job.Format.ToString(), job.RequestedBy, job.Status.ToString(),
            job.RequestedAt, job.CompletedAt, job.FailureReason,
            job.Status == ReportJobStatus.Ready ? $"/api/v1/reports/{job.Id}/download" : null);
}
