using OperationsService.Domain.Model.Aggregates;
using OperationsService.Domain.Model.Queries;

namespace OperationsService.Domain.Services;

public interface IVehicleQueryService
{
    Task<IEnumerable<Vehicle>> Handle(GetVehiclesQuery query);
    Task<Vehicle?> Handle(GetVehicleByIdQuery query);
}
