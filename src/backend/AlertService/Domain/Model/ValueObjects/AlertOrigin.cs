namespace AlertService.Domain.Model.ValueObjects;

/// <summary>Where an alert comes from. Stored alerts without the field are <see cref="Live"/>.</summary>
public enum AlertOrigin
{
    /// <summary>Issued by this service from an assessment or a direct call.</summary>
    Live,

    /// <summary>Given by the cabin by itself and synchronized later from the Edge.</summary>
    Edge
}
