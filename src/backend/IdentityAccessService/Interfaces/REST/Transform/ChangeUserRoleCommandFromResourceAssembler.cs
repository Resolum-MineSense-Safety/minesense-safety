using IdentityAccessService.Domain.Model.Commands;
using IdentityAccessService.Interfaces.REST.Resources;

namespace IdentityAccessService.Interfaces.REST.Transform;

public static class ChangeUserRoleCommandFromResourceAssembler
{
    public static ChangeUserRoleCommand ToCommandFromResource(Guid userId, ChangeUserRoleResource resource) =>
        new(userId, UserRoleParser.Parse(resource.Role));
}
