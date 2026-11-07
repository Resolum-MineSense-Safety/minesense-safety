using IdentityAccessService.Domain.Model.Aggregates;
using IdentityAccessService.Domain.Repositories;
using MineSenseSafety.Shared.Infrastructure.Persistence.InMemory;

namespace IdentityAccessService.Infrastructure.Persistence.InMemory;

public class InMemorySessionRepository : InMemoryRepository<Session>, ISessionRepository
{
    public Task<Session?> FindByRefreshTokenHashAsync(string refreshTokenHash) =>
        Task.FromResult(Store.Values.FirstOrDefault(session => session.RefreshTokenHash == refreshTokenHash));

    public Task<IEnumerable<Session>> FindByUserIdAsync(Guid userId) =>
        Task.FromResult<IEnumerable<Session>>(Store.Values.Where(session => session.UserId == userId).ToList());
}
