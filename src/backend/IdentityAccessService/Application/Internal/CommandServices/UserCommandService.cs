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

    public async Task<User> Handle(SignUpCommand command)
    {
        if (string.IsNullOrEmpty(command.Password) || command.Password.Length < MinimumPasswordLength)
            throw new DomainException($"The password must have at least {MinimumPasswordLength} characters.");
        if (string.IsNullOrWhiteSpace(command.Username))
            throw new DomainException("A user must have a username.");
        if (await userRepository.ExistsByUsernameAsync(command.Username))
            throw new DomainConflictException($"The username '{command.Username}' is already taken.");

        var user = new User(command.Username, command.FullName, command.Role,
            hashingService.HashPassword(command.Password));
        await userRepository.AddAsync(user);
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

    public async Task<User?> Handle(GrantMiningUnitScopeCommand command)
    {
        var user = await userRepository.FindByIdAsync(command.UserId);
        if (user is null) return null;

        user.GrantMiningUnit(command.MiningUnitId);
        await userRepository.UpdateAsync(user);
        return user;
    }

    public async Task<User?> Handle(RevokeMiningUnitScopeCommand command)
    {
        var user = await userRepository.FindByIdAsync(command.UserId);
        if (user is null) return null;

        user.RevokeMiningUnit(command.MiningUnitId);
        await userRepository.UpdateAsync(user);
        return user;
    }
}
