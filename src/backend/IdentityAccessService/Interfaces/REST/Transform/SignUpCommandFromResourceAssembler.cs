using IdentityAccessService.Domain.Model.Commands;
using IdentityAccessService.Interfaces.REST.Resources;

namespace IdentityAccessService.Interfaces.REST.Transform;

public static class SignUpCommandFromResourceAssembler
{
    public static SignUpCommand ToCommandFromResource(SignUpResource resource) =>
        new(resource.Username, resource.FullName, resource.Password, UserRoleParser.Parse(resource.Role));
}
