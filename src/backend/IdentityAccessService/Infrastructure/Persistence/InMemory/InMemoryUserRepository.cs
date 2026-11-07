using IdentityAccessService.Domain.Model.Aggregates;
using IdentityAccessService.Domain.Repositories;
using MineSenseSafety.Shared.Infrastructure.Persistence.InMemory;

namespace IdentityAccessService.Infrastructure.Persistence.InMemory;

public class InMemoryUserRepository : InMemoryRepository<User>, IUserRepository
{
    public Task<User?> FindByUsernameAsync(string username) =>
        Task.FromResult(Store.Values.FirstOrDefault(user => user.Username == Normalize(username)));

    public Task<bool> ExistsByUsernameAsync(string username) =>
        Task.FromResult(Store.Values.Any(user => user.Username == Normalize(username)));

    private static string Normalize(string username) => username.Trim().ToLowerInvariant();
}
