using IdentityAccessService.Domain.Model.Aggregates;
using MineSenseSafety.Shared.Domain.Repositories;

namespace IdentityAccessService.Domain.Repositories;

public interface ISessionRepository : IBaseRepository<Session>
{
    Task<Session?> FindByRefreshTokenHashAsync(string refreshTokenHash);
    Task<IEnumerable<Session>> FindByUserIdAsync(Guid userId);
}
