namespace FeedbackReliabilityService.Application.Internal.IntegrationEvents;

/// <summary>Fields of <c>AlertIssued</c> (Alerts) that Feedback &amp; Reliability needs.</summary>
public record AlertIssuedEvent(Guid AlertId, Guid OperatorId, DateTimeOffset IssuedAt);

/// <summary>Fields of <c>FleetOperatorUpdated</c> (Fleet Monitoring): the mining unit is the operation.</summary>
public record FleetOperatorUpdatedEvent(Guid OperatorId, Guid? MiningUnitId, DateTimeOffset UpdatedAt);
