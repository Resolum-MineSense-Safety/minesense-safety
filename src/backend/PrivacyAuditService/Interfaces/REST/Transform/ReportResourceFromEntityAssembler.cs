using PrivacyAuditService.Domain.Model.Aggregates;
using PrivacyAuditService.Interfaces.REST.Resources;

namespace PrivacyAuditService.Interfaces.REST.Transform;

public static class ReportResourceFromEntityAssembler
{
    public static ReportResource ToResourceFromEntity(ComplianceReport report) =>
        new(report.Id, report.From, report.To, report.SiteId, report.FleetId,
            report.RequestedBy, report.Status.ToString(), report.RequestedAt);
}
