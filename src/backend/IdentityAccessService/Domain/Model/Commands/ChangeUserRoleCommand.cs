using IdentityAccessService.Domain.Model.ValueObjects;

namespace IdentityAccessService.Domain.Model.Commands;

public record ChangeUserRoleCommand(Guid UserId, UserRole Role);
