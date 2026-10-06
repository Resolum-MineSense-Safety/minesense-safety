using System.Net.Mime;
using IdentityAccessService.Domain.Services;
using IdentityAccessService.Interfaces.REST.Resources;
using IdentityAccessService.Interfaces.REST.Transform;
using Microsoft.AspNetCore.Mvc;

namespace IdentityAccessService.Interfaces.REST;

[ApiController]
[Route("api/v1/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
public class AuthenticationController(IUserCommandService userCommandService) : ControllerBase
{
    [HttpPost("sign-up")]
    public async Task<IActionResult> SignUp([FromBody] SignUpResource resource)
    {
        var command = SignUpCommandFromResourceAssembler.ToCommandFromResource(resource);
        var user = await userCommandService.Handle(command);
        var userResource = UserResourceFromEntityAssembler.ToResourceFromEntity(user);
        return CreatedAtAction(nameof(UsersController.GetUserById), "Users", new { userId = user.Id }, userResource);
    }

    /// <summary>
    /// Verifies the credentials of an active user.
    /// TODO: issue JWT access token.
    /// </summary>
    [HttpPost("sign-in")]
    public async Task<IActionResult> SignIn([FromBody] SignInResource resource)
    {
        var command = SignInCommandFromResourceAssembler.ToCommandFromResource(resource);
        var user = await userCommandService.Handle(command);
        return Ok(AuthenticatedUserResourceFromEntityAssembler.ToResourceFromEntity(user));
    }
}
