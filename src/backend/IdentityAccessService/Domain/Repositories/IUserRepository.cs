using IdentityAccessService.Domain.Model.Aggregates;
using MineSenseSafety.Shared.Domain.Repositories;

namespace IdentityAccessService.Domain.Repositories;

public interface IUserRepository : IBaseRepository<User>
{
    Task<User?> FindByUsernameAsync(string username);
    Task<bool> ExistsByUsernameAsync(string username);
}
