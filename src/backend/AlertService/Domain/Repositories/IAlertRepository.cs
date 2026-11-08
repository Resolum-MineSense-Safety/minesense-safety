using AlertService.Domain.Model.Aggregates;
using AlertService.Domain.Model.ValueObjects;
using MineSenseSafety.Shared.Domain.Repositories;

namespace AlertService.Domain.Repositories;

public interface IAlertRepository : IBaseRepository<Alert>
{
    Task<IEnumerable<Alert>> FindByOperatorIdAsync(Guid operatorId);

    /// <summary>Alert already issued for the given fatigue assessment, if any.</summary>
    Task<Alert?> FindByAssessmentIdAsync(Guid assessmentId);

    /// <summary>Returns the alerts in the given status, oldest first.</summary>
    Task<IEnumerable<Alert>> FindByStatusAsync(AlertStatus status);
}
