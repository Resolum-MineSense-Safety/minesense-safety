namespace IdentityAccessService.Domain.Model.Commands;

public record GrantMiningUnitScopeCommand(Guid UserId, Guid MiningUnitId);
