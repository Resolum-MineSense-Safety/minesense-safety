using OperationsService.Domain.Model.Aggregates;
using OperationsService.Domain.Model.Queries;
using OperationsService.Domain.Repositories;
using OperationsService.Domain.Services;

namespace OperationsService.Application.Internal.QueryServices;

public class OperatorQueryService(IOperatorRepository operatorRepository) : IOperatorQueryService
{
    public Task<IEnumerable<Operator>> Handle(GetOperatorsQuery query) =>
        query.MiningUnitId is { } miningUnitId
            ? operatorRepository.FindByMiningUnitIdAsync(miningUnitId)
            : operatorRepository.ListAsync();

    public Task<Operator?> Handle(GetOperatorByIdQuery query) =>
        operatorRepository.FindByIdAsync(query.OperatorId);
}
