using DeviceManagementService.Domain.Model.Aggregates;
using MineSenseSafety.Shared.Domain.Repositories;

namespace DeviceManagementService.Domain.Repositories;

public interface IPreShiftCheckRepository : IBaseRepository<PreShiftCheck>
{
    Task<PreShiftCheck?> FindLatestByOperatorIdAsync(Guid operatorId);
}
