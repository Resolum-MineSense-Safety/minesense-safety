namespace AlertService.Domain.Model.Commands;

/// <summary>Escalates every issued alert whose acknowledgement deadline has passed.</summary>
public record EscalateOverdueAlertsCommand;
