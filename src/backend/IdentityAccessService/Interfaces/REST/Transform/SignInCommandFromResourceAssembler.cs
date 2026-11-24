using IdentityAccessService.Domain.Model.Commands;
using IdentityAccessService.Interfaces.REST.Resources;

namespace IdentityAccessService.Interfaces.REST.Transform;

public static class SignInCommandFromResourceAssembler
{
    public static SignInCommand ToCommandFromResource(SignInResource resource) =>
        new(resource.Username, resource.Password);
}
