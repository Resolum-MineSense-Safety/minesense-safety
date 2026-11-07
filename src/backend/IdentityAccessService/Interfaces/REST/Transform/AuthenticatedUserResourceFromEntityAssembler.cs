using IdentityAccessService.Domain.Model.ValueObjects;
using IdentityAccessService.Interfaces.REST.Resources;

namespace IdentityAccessService.Interfaces.REST.Transform;

public static class AuthenticatedUserResourceFromEntityAssembler
{
    public static AuthenticatedUserResource ToResourceFromEntity(AuthenticatedSession session, DateTimeOffset now)
    {
        var user = session.User;
        var expiresIn = (int)Math.Max(0, (session.AccessTokenExpiresAt - now).TotalSeconds);
        return new AuthenticatedUserResource(user.Id, user.Username, user.FullName, user.Role.ToString(),
            user.MiningUnitIds.Order().ToList(), session.AccessToken, "Bearer", expiresIn,
            session.AccessTokenExpiresAt, session.RefreshToken, session.RefreshTokenExpiresAt);
    }
}
