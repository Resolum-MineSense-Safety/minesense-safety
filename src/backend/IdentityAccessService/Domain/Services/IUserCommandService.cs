using IdentityAccessService.Domain.Model.Aggregates;
using IdentityAccessService.Domain.Model.Commands;

namespace IdentityAccessService.Domain.Services;

public interface IUserCommandService
{
    Task<User> Handle(SignUpCommand command);
    Task<User> Handle(SignInCommand command);
    Task<User?> Handle(ChangeUserRoleCommand command);
    Task<User?> Handle(DeactivateUserCommand command);
}
