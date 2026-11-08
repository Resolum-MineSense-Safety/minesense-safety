namespace FatigueDetectionService.Domain.Model.ValueObjects;

/// <summary>Where an assessment comes from. Stored assessments without the field are <see cref="Live"/>.</summary>
public enum AssessmentOrigin
{
    /// <summary>Evaluated by this service when the signals arrived.</summary>
    Live,

    /// <summary>Captured earlier in a cabin and synchronized in a batch: a historical record.</summary>
    Edge
}
