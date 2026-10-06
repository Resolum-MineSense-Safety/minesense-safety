using AlertService.Domain.Model.Aggregates;
using MineSenseSafety.Shared.Domain.Repositories;

namespace AlertService.Domain.Repositories;

public interface IAlertRepository : IBaseRepository<Alert>
{
    Task<IEnumerable<Alert>> FindByOperatorIdAsync(Guid operatorId);
}
