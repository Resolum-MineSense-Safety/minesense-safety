namespace IncidentManagementService.Domain.Model.ValueObjects;

/// <summary>Immutable entry of the incident history: what happened, who did it and when.</summary>
public record IncidentEvent(IncidentEventType Type, Guid? ActorId, DateTimeOffset OccurredAt, string Detail);
