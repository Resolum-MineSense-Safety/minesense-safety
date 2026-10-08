using DeviceManagementService.Domain.Model.Aggregates;
using DeviceManagementService.Domain.Model.Queries;

namespace DeviceManagementService.Domain.Services;

public interface IPreShiftCheckQueryService
{
    Task<PreShiftCheck?> Handle(GetPreShiftCheckByIdQuery query);
    Task<PreShiftCheck?> Handle(GetLatestPreShiftCheckByOperatorIdQuery query);
}
