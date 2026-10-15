using AnalyticsService.Domain.Model.Commands;
using AnalyticsService.Domain.Model.ValueObjects;
using MineSenseSafety.Shared.Domain.Model;

namespace AnalyticsService.Domain.Model.Aggregates;

/// <summary>
/// Historical observation of one fatigue alert with its operational context.
/// It is the raw material of the trends and measure evaluations (EP11).
/// </summary>
public class FatigueMetric : AggregateRoot
{
    public Guid OperatorId { get; private set; }
    public Guid AlertId { get; private set; }
    public FatigueSeverity Severity { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }
    public OperationalContext Context { get; private set; }

    public FatigueMetric(RecordFatigueMetricCommand command)
    {
        if (command.OperatorId == Guid.Empty)
            throw new DomainException("A fatigue metric must belong to an operator.");
        if (command.AlertId == Guid.Empty)
            throw new DomainException("A fatigue metric must reference its alert.");

        OperatorId = command.OperatorId;
        AlertId = command.AlertId;
        Severity = command.Severity;
        OccurredAt = command.OccurredAt;
        Context = new OperationalContext(command.Shift, command.MineSite, command.Location, command.Fleet);
    }
}
