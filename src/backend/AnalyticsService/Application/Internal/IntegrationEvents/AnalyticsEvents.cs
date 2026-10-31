namespace AnalyticsService.Application.Internal.IntegrationEvents;

/// <summary>Fields of <c>AlertIssued</c> (Alerts) that Analytics needs.</summary>
public record AlertIssuedEvent(Guid AlertId, Guid OperatorId, string Severity, DateTimeOffset IssuedAt);

/// <summary>Fields of <c>FleetOperatorUpdated</c> (Fleet Monitoring) that Analytics needs.</summary>
public record FleetOperatorUpdatedEvent(
    Guid OperatorId,
    string? Shift,
    Guid? MiningUnitId,
    string? Location,
    string? Fleet,
    DateTimeOffset UpdatedAt);
