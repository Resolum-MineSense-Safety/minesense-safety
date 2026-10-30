using MineSenseSafety.Shared.Domain.Model;

namespace AnalyticsService.Domain.Model.ValueObjects;

/// <summary>Half-open time window [From, To).</summary>
public record AnalysisPeriod
{
    public DateTimeOffset From { get; }
    public DateTimeOffset To { get; }

    public AnalysisPeriod(DateTimeOffset from, DateTimeOffset to)
    {
        if (to <= from)
            throw new DomainException("The analysis period must end after it starts.");
        From = from;
        To = to;
    }

    public TimeSpan Length => To - From;

    public bool Contains(DateTimeOffset moment) => moment >= From && moment < To;
}
