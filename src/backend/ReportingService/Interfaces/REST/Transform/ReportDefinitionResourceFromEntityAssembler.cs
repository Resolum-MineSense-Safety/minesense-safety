using ReportingService.Domain.Model.Aggregates;
using ReportingService.Interfaces.REST.Resources;

namespace ReportingService.Interfaces.REST.Transform;

public static class ReportDefinitionResourceFromEntityAssembler
{
    public static ReportDefinitionResource ToResourceFromEntity(ReportDefinition definition) =>
        new(definition.Id, definition.Code, definition.Name, definition.Version,
            definition.DataSources, definition.IsActive);
}
