using ReportingService.Domain.Model.Commands;
using ReportingService.Domain.Model.ValueObjects;
using ReportingService.Interfaces.REST.Resources;
using MineSenseSafety.Shared.Domain.Model;

namespace ReportingService.Interfaces.REST.Transform;

public static class RequestReportGenerationCommandFromResourceAssembler
{
    /// <param name="authenticatedUserId">User id of the caller's token, if any.</param>
    public static RequestReportGenerationCommand ToCommandFromResource(
        RequestSafetyReportResource resource, Guid? authenticatedUserId = null)
    {
        if (!Enum.TryParse<ReportFormat>(resource.Format, ignoreCase: true, out var format))
            throw new DomainException($"Unknown report format '{resource.Format}'.");

        var requestedBy = resource.RequestedBy ?? authenticatedUserId
            ?? throw new DomainException("The report request must say who requests it (requestedBy).");

        return new RequestReportGenerationCommand(resource.From, resource.To, resource.SiteId,
            resource.FleetId, resource.DefinitionCode, format, requestedBy);
    }
}
