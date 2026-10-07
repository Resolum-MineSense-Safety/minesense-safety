using OperationsService.Domain.Model.Aggregates;
using OperationsService.Domain.Model.Commands;

namespace OperationsService.Domain.Services;

public interface IVehicleCommandService
{
    Task<Vehicle> Handle(RegisterVehicleCommand command);
    Task<Vehicle?> Handle(UpdateVehicleCommand command);
}
