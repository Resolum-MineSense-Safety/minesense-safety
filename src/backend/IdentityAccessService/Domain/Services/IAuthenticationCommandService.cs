using IdentityAccessService.Domain.Model.Commands;
using IdentityAccessService.Domain.Model.ValueObjects;

namespace IdentityAccessService.Domain.Services;

public interface IAuthenticationCommandService
{
    /// <summary>Opens a session, or returns null when the credentials are invalid or the user is inactive.</summary>
    Task<AuthenticatedSession?> Handle(SignInCommand command);

    /// <summary>Rotates the refresh token, or returns null when it is unknown, expired or revoked.</summary>
    Task<AuthenticatedSession?> Handle(RefreshSessionCommand command);

    Task Handle(SignOutCommand command);
}
