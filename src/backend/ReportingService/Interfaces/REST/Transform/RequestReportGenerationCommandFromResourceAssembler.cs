using ReportingService.Domain.Model.Commands;
using ReportingService.Domain.Model.ValueObjects;
using ReportingService.Interfaces.REST.Resources;
using MineSenseSafety.Shared.Domain.Model;

namespace ReportingService.Interfaces.REST.Transform;

public static class RequestReportGenerationCommandFromResourceAssembler
{
    /// <param name="authenticatedUserId">
    /// User id of the caller's token, if any. It wins over the body so an authenticated caller
    /// cannot record the report in someone else's name.
    /// </param>
    public static RequestReportGenerationCommand ToCommandFromResource(
        RequestSafetyReportResource resource, Guid? authenticatedUserId = null)
    {
        if (!Enum.TryParse<ReportFormat>(resource.Format, ignoreCase: true, out var format))
            throw new DomainException($"Unknown report format '{resource.Format}'.");

        var requestedBy = authenticatedUserId ?? resource.RequestedBy
            ?? throw new DomainException("The report request must say who requests it (requestedBy).");

        return new RequestReportGenerationCommand(resource.From, resource.To, resource.SiteId,
            resource.FleetId, resource.DefinitionCode, format, requestedBy);
    }
}
