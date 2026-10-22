using IdentityAccessService.Domain.Model.ValueObjects;

namespace IdentityAccessService.Domain.Model.Queries;

public record GetUsersByRoleQuery(UserRole Role);
