using System.Text.Json.Serialization;

namespace IdentityAccessService.Interfaces.REST.Resources;

public record MiningUnitScopeResource([property: JsonRequired] Guid MiningUnitId);
