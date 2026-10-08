using IdentityAccessService.Domain.Model.Aggregates;
using IdentityAccessService.Interfaces.REST.Resources;

namespace IdentityAccessService.Interfaces.REST.Transform;

public static class AuthenticatedUserResourceFromEntityAssembler
{
    public static AuthenticatedUserResource ToResourceFromEntity(User user) =>
        new(user.Id, user.Username, user.Role.ToString());
}
