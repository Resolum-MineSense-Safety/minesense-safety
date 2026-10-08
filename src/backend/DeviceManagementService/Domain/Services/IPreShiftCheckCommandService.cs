using DeviceManagementService.Domain.Model.Aggregates;
using DeviceManagementService.Domain.Model.Commands;

namespace DeviceManagementService.Domain.Services;

public interface IPreShiftCheckCommandService
{
    Task<PreShiftCheck> Handle(RunPreShiftCheckCommand command);
}
