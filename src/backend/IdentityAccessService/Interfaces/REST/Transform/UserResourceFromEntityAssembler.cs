using IdentityAccessService.Domain.Model.Aggregates;
using IdentityAccessService.Interfaces.REST.Resources;

namespace IdentityAccessService.Interfaces.REST.Transform;

public static class UserResourceFromEntityAssembler
{
    public static UserResource ToResourceFromEntity(User user) =>
        new(user.Id, user.Username, user.FullName, user.Role.ToString(), user.IsActive);
}
