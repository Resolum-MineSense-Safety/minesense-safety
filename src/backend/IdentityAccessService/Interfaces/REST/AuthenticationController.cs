using System.Net.Mime;
using IdentityAccessService.Domain.Model.Commands;
using IdentityAccessService.Domain.Model.ValueObjects;
using IdentityAccessService.Domain.Services;
using IdentityAccessService.Interfaces.REST.Resources;
using IdentityAccessService.Interfaces.REST.Transform;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdentityAccessService.Interfaces.REST;

[ApiController]
[AllowAnonymous]
[Route("api/v1/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
public class AuthenticationController(
    IUserCommandService userCommandService,
    IAuthenticationCommandService authenticationCommandService,
    TimeProvider timeProvider) : ControllerBase
{
    private static readonly object InvalidCredentials = new { message = "Invalid credentials." };
    private static readonly object InvalidRefreshToken = new { message = "The refresh token is invalid, expired or revoked." };

    /// <summary>
    /// Self-registration. Only an authenticated administrator may register another administrator.
    /// </summary>
    [HttpPost("sign-up")]
    [ProducesResponseType(typeof(UserResource), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> SignUp([FromBody] SignUpResource resource)
    {
        var command = SignUpCommandFromResourceAssembler.ToCommandFromResource(resource);
        if (command.Role == UserRole.Administrator && !User.IsInRole(nameof(UserRole.Administrator)))
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "Only an administrator can register another administrator." });

        var user = await userCommandService.Handle(command);
        var userResource = UserResourceFromEntityAssembler.ToResourceFromEntity(user);
        return CreatedAtAction(nameof(UsersController.GetUserById), "Users", new { userId = user.Id }, userResource);
    }

    /// <summary>TS-01: verifies the credentials of an active user and issues an access and a refresh token.</summary>
    [HttpPost("sign-in")]
    [HttpPost("/api/v1/auth/login")]
    [ProducesResponseType(typeof(AuthenticatedUserResource), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SignIn([FromBody] SignInResource resource)
    {
        var command = SignInCommandFromResourceAssembler.ToCommandFromResource(resource);
        var session = await authenticationCommandService.Handle(command);
        if (session is null) return Unauthorized(InvalidCredentials);
        return Ok(AuthenticatedUserResourceFromEntityAssembler.ToResourceFromEntity(session, timeProvider.GetUtcNow()));
    }

    /// <summary>TS-02: exchanges a refresh token for a new pair; the old refresh token stops working.</summary>
    [HttpPost("refresh")]
    [HttpPost("/api/v1/auth/refresh")]
    [ProducesResponseType(typeof(AuthenticatedUserResource), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenResource resource)
    {
        var session = await authenticationCommandService.Handle(new RefreshSessionCommand(resource.RefreshToken));
        if (session is null) return Unauthorized(InvalidRefreshToken);
        return Ok(AuthenticatedUserResourceFromEntityAssembler.ToResourceFromEntity(session, timeProvider.GetUtcNow()));
    }

    /// <summary>Ends the session of the given refresh token. Unknown tokens are ignored.</summary>
    [HttpPost("sign-out")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SignOut([FromBody] RefreshTokenResource resource)
    {
        await authenticationCommandService.Handle(new SignOutCommand(resource.RefreshToken));
        return NoContent();
    }
}
