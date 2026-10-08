using DeviceManagementService.Domain.Model.Aggregates;
using DeviceManagementService.Domain.Model.Queries;
using DeviceManagementService.Domain.Repositories;
using DeviceManagementService.Domain.Services;

namespace DeviceManagementService.Application.Internal.QueryServices;

public class PreShiftCheckQueryService(IPreShiftCheckRepository preShiftCheckRepository) : IPreShiftCheckQueryService
{
    public Task<PreShiftCheck?> Handle(GetPreShiftCheckByIdQuery query) =>
        preShiftCheckRepository.FindByIdAsync(query.PreShiftCheckId);

    public Task<PreShiftCheck?> Handle(GetLatestPreShiftCheckByOperatorIdQuery query) =>
        preShiftCheckRepository.FindLatestByOperatorIdAsync(query.OperatorId);
}
