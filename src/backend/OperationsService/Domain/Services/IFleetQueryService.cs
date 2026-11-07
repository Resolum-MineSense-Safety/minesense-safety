using OperationsService.Domain.Model.Aggregates;
using OperationsService.Domain.Model.Queries;

namespace OperationsService.Domain.Services;

public interface IFleetQueryService
{
    Task<Fleet?> Handle(GetFleetByIdQuery query);
    Task<IEnumerable<Fleet>> Handle(GetFleetsByMiningUnitQuery query);
}
