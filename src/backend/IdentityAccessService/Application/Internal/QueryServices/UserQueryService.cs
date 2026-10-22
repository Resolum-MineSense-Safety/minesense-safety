using IdentityAccessService.Domain.Model.Aggregates;
using IdentityAccessService.Domain.Model.Queries;
using IdentityAccessService.Domain.Repositories;
using IdentityAccessService.Domain.Services;

namespace IdentityAccessService.Application.Internal.QueryServices;

public class UserQueryService(IUserRepository userRepository) : IUserQueryService
{
    public Task<User?> Handle(GetUserByIdQuery query) =>
        userRepository.FindByIdAsync(query.UserId);

    public Task<IEnumerable<User>> Handle(GetAllUsersQuery query) =>
        userRepository.ListAsync();

    public async Task<IEnumerable<User>> Handle(GetUsersByRoleQuery query) =>
        (await userRepository.ListAsync()).Where(user => user.Role == query.Role).ToList();
}
