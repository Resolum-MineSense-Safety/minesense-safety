using PrivacyAuditService.Domain.Model.Commands;
using PrivacyAuditService.Interfaces.REST.Resources;

namespace PrivacyAuditService.Interfaces.REST.Transform;

public static class RequestSafetyReportCommandFromResourceAssembler
{
    public static RequestSafetyReportCommand ToCommandFromResource(RequestSafetyReportResource resource) =>
        new(resource.From, resource.To, resource.SiteId, resource.FleetId,
            resource.RequestedBy ?? Guid.Empty);
}
