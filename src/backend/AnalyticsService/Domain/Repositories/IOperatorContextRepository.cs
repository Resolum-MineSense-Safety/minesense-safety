using AnalyticsService.Domain.Model.ReadModels;

namespace AnalyticsService.Domain.Repositories;

public interface IOperatorContextRepository
{
    Task<OperatorContext?> FindByOperatorIdAsync(Guid operatorId);

    /// <summary>Stores the context unless a newer one for the same operator is already kept.</summary>
    Task UpsertAsync(OperatorContext context);
}
