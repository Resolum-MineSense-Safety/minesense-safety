using ReportingService.Domain.Model.Commands;
using ReportingService.Interfaces.REST.Resources;
using MineSenseSafety.Shared.Domain.Model;

namespace ReportingService.Interfaces.REST.Transform;

public static class DefineReportIndicatorCommandFromResourceAssembler
{
    public static DefineReportIndicatorCommand ToCommandFromResource(DefineReportIndicatorResource resource)
    {
        if (string.IsNullOrWhiteSpace(resource.Code))
            throw new DomainException("A report definition must have a code.");
        if (string.IsNullOrWhiteSpace(resource.Name))
            throw new DomainException("A report definition must have a name.");
        if (string.IsNullOrWhiteSpace(resource.Version))
            throw new DomainException("A report definition must have a version.");
        if (resource.DataSources is null || !resource.DataSources.Any())
            throw new DomainException("A report definition must declare at least one data source.");

        return new DefineReportIndicatorCommand(resource.Code, resource.Name, resource.Version, resource.DataSources);
    }
}
