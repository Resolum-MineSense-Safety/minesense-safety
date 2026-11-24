using IdentityAccessService.Domain.Model.Aggregates;
using IdentityAccessService.Domain.Model.Queries;

namespace IdentityAccessService.Domain.Services;

public interface IUserQueryService
{
    Task<User?> Handle(GetUserByIdQuery query);
    Task<IEnumerable<User>> Handle(GetAllUsersQuery query);
    Task<IEnumerable<User>> Handle(GetUsersByRoleQuery query);
}
