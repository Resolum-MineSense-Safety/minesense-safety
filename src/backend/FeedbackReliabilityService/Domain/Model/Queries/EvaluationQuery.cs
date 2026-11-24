using MineSenseSafety.Shared.Domain.Model;

namespace FeedbackReliabilityService.Domain.Model.Queries;

/// <summary>Period is [From, To), based on the original alert issuance time.</summary>
public record EvaluationQuery(Guid OperationId, DateTimeOffset From, DateTimeOffset To)
{
    public void Validate()
    {
        if (OperationId == Guid.Empty) throw new DomainException("Operation is required.");
        if (From >= To || To - From > TimeSpan.FromDays(366))
            throw new DomainException("The period must be increasing and no longer than 366 days.");
    }
}
