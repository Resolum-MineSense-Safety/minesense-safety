namespace AnalyticsService.Domain.Model.ValueObjects;

/// <summary>Kinds of measures whose effect is evaluated (US25).</summary>
public enum PreventiveMeasureType
{
    PreventivePause,
    OperatorRelief,
    ShiftReorganization,
    Other
}
