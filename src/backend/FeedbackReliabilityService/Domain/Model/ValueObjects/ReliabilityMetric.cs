using FeedbackReliabilityService.Domain.Model.Aggregates;
using FeedbackReliabilityService.Domain.Model.Queries;

namespace FeedbackReliabilityService.Domain.Model.ValueObjects;

public record ReliabilityMetric(Guid OperationId, DateTimeOffset From, DateTimeOffset To,
    int TotalAlerts, int PendingAlerts, int SampleSize, int Confirmed, int FalseAlarms, int Inconclusive,
    double ConfirmedProportion, double FalseAlarmProportion, double InconclusiveProportion,
    int MinimumSampleSize, bool InsufficientSample)
{
    /// <summary>Each alert contributes once; the last supervisor review takes precedence.</summary>
    public static ReliabilityMetric Calculate(EvaluationQuery query, IEnumerable<AlertEvaluation> alerts, int minimumSampleSize)
    {
        query.Validate();
        if (minimumSampleSize < 1) throw new ArgumentOutOfRangeException(nameof(minimumSampleSize));
        var classifications = alerts.Where(alert => alert.OperationId == query.OperationId &&
                alert.IssuedAt >= query.From && alert.IssuedAt < query.To)
            .Select(alert => alert.EffectiveClassification).ToArray();
        var confirmed = classifications.Count(item => item == FeedbackClassification.Confirmed);
        var falseAlarms = classifications.Count(item => item == FeedbackClassification.FalseAlarm);
        var inconclusive = classifications.Count(item => item == FeedbackClassification.Inconclusive);
        var sample = confirmed + falseAlarms + inconclusive;
        double Proportion(int count) => sample == 0 ? 0 : (double)count / sample;
        return new(query.OperationId, query.From, query.To, classifications.Length, classifications.Length - sample,
            sample, confirmed, falseAlarms, inconclusive, Proportion(confirmed), Proportion(falseAlarms),
            Proportion(inconclusive), minimumSampleSize, sample < minimumSampleSize);
    }
}
