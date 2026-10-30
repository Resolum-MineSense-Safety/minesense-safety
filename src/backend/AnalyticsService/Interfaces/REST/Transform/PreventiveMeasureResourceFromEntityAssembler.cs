using AnalyticsService.Domain.Model.Aggregates;
using AnalyticsService.Interfaces.REST.Resources;

namespace AnalyticsService.Interfaces.REST.Transform;

public static class PreventiveMeasureResourceFromEntityAssembler
{
    public static PreventiveMeasureResource ToResourceFromEntity(PreventiveMeasure measure) =>
        new(measure.Id, measure.Name, measure.MeasureType.ToString(), measure.Description, measure.AppliedAt,
            measure.RegisteredBy, measure.Scope.Shift, measure.Scope.MineSite, measure.Scope.Location,
            measure.Scope.Fleet);
}
