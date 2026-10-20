using IdentityAccessService.Application.Internal.OutboundServices;
using IdentityAccessService.Domain.Model.Aggregates;
using IdentityAccessService.Domain.Model.Commands;
using IdentityAccessService.Domain.Repositories;
using IdentityAccessService.Domain.Services;
using MineSenseSafety.Shared.Domain.Model;

namespace IdentityAccessService.Application.Internal.CommandServices;

public class UserCommandService(IUserRepository userRepository, IHashingService hashingService) : IUserCommandService
{
    private const int MinimumPasswordLength = 8;
    private const string InvalidCredentialsMessage = "Invalid credentials.";

    public async Task<User> Handle(SignUpCommand command)
    {
        if (string.IsNullOrEmpty(command.Password) || command.Password.Length < MinimumPasswordLength)
            throw new DomainException($"The password must have at least {MinimumPasswordLength} characters.");
        if (await userRepository.ExistsByUsernameAsync(command.Username))
            throw new DomainException($"The username '{command.Username}' is already taken.");

        var user = new User(command.Username, command.FullName, command.Role,
            hashingService.HashPassword(command.Password));
        await userRepository.AddAsync(user);
        return user;
    }

    public async Task<User> Handle(SignInCommand command)
    {
        var user = await userRepository.FindByUsernameAsync(command.Username);
        if (user is null || !user.IsActive || !hashingService.VerifyPassword(command.Password, user.PasswordHash))
            throw new DomainException(InvalidCredentialsMessage);

        return user;
    }

    public async Task<User?> Handle(ChangeUserRoleCommand command)
    {
        var user = await userRepository.FindByIdAsync(command.UserId);
        if (user is null) return null;

        user.ChangeRole(command.Role);
        await userRepository.UpdateAsync(user);
        return user;
    }

    public async Task<User?> Handle(DeactivateUserCommand command)
    {
        var user = await userRepository.FindByIdAsync(command.UserId);
        if (user is null) return null;

        user.Deactivate();
        await userRepository.UpdateAsync(user);
        return user;
    }
}
