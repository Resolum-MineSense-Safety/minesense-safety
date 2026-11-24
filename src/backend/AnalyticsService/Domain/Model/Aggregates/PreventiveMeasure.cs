using AnalyticsService.Domain.Model.Commands;
using AnalyticsService.Domain.Model.ValueObjects;
using MineSenseSafety.Shared.Domain.Model;

namespace AnalyticsService.Domain.Model.Aggregates;

/// <summary>
/// Pause, relief or work reorganization applied to part of the operation,
/// registered so its effect can be compared before and after (US25).
/// </summary>
public class PreventiveMeasure : AggregateRoot
{
    public string Name { get; private set; }
    public PreventiveMeasureType MeasureType { get; private set; }
    public string Description { get; private set; }
    public DateTimeOffset AppliedAt { get; private set; }
    public ContextFilter Scope { get; private set; }
    public Guid RegisteredBy { get; private set; }

    public PreventiveMeasure(RegisterPreventiveMeasureCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Name))
            throw new DomainException("A preventive measure needs a name.");
        if (command.RegisteredBy == Guid.Empty)
            throw new DomainException("A preventive measure must identify who registered it.");

        Name = command.Name.Trim();
        MeasureType = command.MeasureType;
        Description = command.Description?.Trim() ?? string.Empty;
        AppliedAt = command.AppliedAt;
        Scope = command.Scope;
        RegisteredBy = command.RegisteredBy;
    }
}
