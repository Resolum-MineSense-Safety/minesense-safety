using AnalyticsService.Domain.Model.Commands;
using AnalyticsService.Domain.Model.ValueObjects;
using AnalyticsService.Interfaces.REST.Resources;
using MineSenseSafety.Shared.Domain.Model;

namespace AnalyticsService.Interfaces.REST.Transform;

public static class RecordFatigueMetricCommandFromResourceAssembler
{
    public static RecordFatigueMetricCommand ToCommandFromResource(RecordFatigueEventResource resource)
    {
        if (!Enum.TryParse<FatigueSeverity>(resource.Severity, ignoreCase: true, out var severity))
            throw new DomainException($"Unknown fatigue severity '{resource.Severity}'.");

        return new RecordFatigueMetricCommand(resource.OperatorId, resource.AlertId, severity, resource.OccurredAt,
            resource.Shift, resource.MineSite, resource.Location, resource.Fleet);
    }
}
