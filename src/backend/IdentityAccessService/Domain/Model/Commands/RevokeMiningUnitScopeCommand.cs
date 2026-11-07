namespace IdentityAccessService.Domain.Model.Commands;

public record RevokeMiningUnitScopeCommand(Guid UserId, Guid MiningUnitId);
