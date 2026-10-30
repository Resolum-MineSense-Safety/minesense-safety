using ReportingService.Domain.Model.Commands;
using ReportingService.Domain.Model.ValueObjects;
using ReportingService.Interfaces.REST.Resources;
using MineSenseSafety.Shared.Domain.Model;

namespace ReportingService.Interfaces.REST.Transform;

public static class RequestReportGenerationCommandFromResourceAssembler
{
    public static RequestReportGenerationCommand ToCommandFromResource(RequestSafetyReportResource resource)
    {
        if (!Enum.TryParse<ReportFormat>(resource.Format, ignoreCase: true, out var format))
            throw new DomainException($"Unknown report format '{resource.Format}'.");

        return new RequestReportGenerationCommand(resource.From, resource.To, resource.SiteId,
            resource.FleetId, resource.DefinitionCode, format, Guid.Empty);
    }
}
