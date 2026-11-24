namespace FeedbackReliabilityService.Application.Internal.OutboundServices;

public record SourceAlert(Guid Id, Guid OperatorId, DateTimeOffset IssuedAt);

/// <summary>Anti-corruption port; never references the AlertService project or database.</summary>
public interface IAlertSource
{
    Task<SourceAlert?> FindAsync(Guid alertId, CancellationToken cancellationToken);
}
