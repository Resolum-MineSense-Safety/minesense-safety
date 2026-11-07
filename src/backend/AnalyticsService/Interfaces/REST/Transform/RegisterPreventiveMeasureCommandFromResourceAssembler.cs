using AnalyticsService.Domain.Model.Commands;
using AnalyticsService.Domain.Model.ValueObjects;
using AnalyticsService.Interfaces.REST.Resources;
using MineSenseSafety.Shared.Domain.Model;

namespace AnalyticsService.Interfaces.REST.Transform;

public static class RegisterPreventiveMeasureCommandFromResourceAssembler
{
    public static RegisterPreventiveMeasureCommand ToCommandFromResource(RegisterPreventiveMeasureResource resource)
    {
        if (!Enum.TryParse<PreventiveMeasureType>(resource.MeasureType, ignoreCase: true, out var measureType))
            throw new DomainException($"Unknown preventive measure type '{resource.MeasureType}'.");

        return new RegisterPreventiveMeasureCommand(resource.Name, measureType, resource.Description,
            resource.AppliedAt, new ContextFilter(resource.Shift, resource.MineSite, resource.Location, resource.Fleet),
            resource.RegisteredBy);
    }
}
