using IdentityAccessService.Domain.Model.ValueObjects;
using MineSenseSafety.Shared.Domain.Model;

namespace IdentityAccessService.Interfaces.REST.Transform;

public static class UserRoleParser
{
    public static UserRole Parse(string role)
    {
        if (!Enum.TryParse<UserRole>(role, ignoreCase: true, out var userRole) || !Enum.IsDefined(userRole))
            throw new DomainException($"Unknown user role '{role}'.");

        return userRole;
    }
}
