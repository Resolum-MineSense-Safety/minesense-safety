using System.Net.Mime;
using IdentityAccessService.Domain.Model.Aggregates;
using IdentityAccessService.Domain.Model.Commands;
using IdentityAccessService.Domain.Model.Queries;
using IdentityAccessService.Domain.Model.ValueObjects;
using IdentityAccessService.Domain.Services;
using IdentityAccessService.Interfaces.REST.Resources;
using IdentityAccessService.Interfaces.REST.Transform;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;

namespace IdentityAccessService.Interfaces.REST;

[ApiController]
[Route("api/v1/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
public class UsersController(
    IUserCommandService userCommandService,
    IUserQueryService userQueryService) : ControllerBase
{
    private const string Administrator = nameof(UserRole.Administrator);

    /// <summary>TS-03: profile, role and mining unit scopes of the signed-in user.</summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserResource), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetCurrentUser()
    {
        if (!Guid.TryParse(User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out var userId)) return Unauthorized();

        var user = await userQueryService.Handle(new GetUserByIdQuery(userId));
        if (user is null) return Unauthorized();
        if (!user.IsActive)
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "The user is deactivated." });
        return Ok(UserResourceFromEntityAssembler.ToResourceFromEntity(user));
    }

    /// <summary>TS-04: an administrator registers a user with any role.</summary>
    [HttpPost]
    [Authorize(Roles = Administrator)]
    [ProducesResponseType(typeof(UserResource), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreateUser([FromBody] SignUpResource resource)
    {
        var user = await userCommandService.Handle(SignUpCommandFromResourceAssembler.ToCommandFromResource(resource));
        return CreatedAtAction(nameof(GetUserById), new { userId = user.Id },
            UserResourceFromEntityAssembler.ToResourceFromEntity(user));
    }

    [HttpGet]
    [Authorize(Roles = Administrator)]
    [ProducesResponseType(typeof(IEnumerable<UserResource>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsers([FromQuery] string? role)
    {
        IEnumerable<User> users = string.IsNullOrWhiteSpace(role)
            ? await userQueryService.Handle(new GetAllUsersQuery())
            : await userQueryService.Handle(new GetUsersByRoleQuery(UserRoleParser.Parse(role)));
        return Ok(users.Select(UserResourceFromEntityAssembler.ToResourceFromEntity));
    }

    [HttpGet("{userId:guid}")]
    [Authorize(Roles = Administrator)]
    [ProducesResponseType(typeof(UserResource), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserById(Guid userId)
    {
        var user = await userQueryService.Handle(new GetUserByIdQuery(userId));
        if (user is null) return NotFound();
        return Ok(UserResourceFromEntityAssembler.ToResourceFromEntity(user));
    }

    [HttpPut("{userId:guid}/role")]
    [Authorize(Roles = Administrator)]
    [ProducesResponseType(typeof(UserResource), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangeUserRole(Guid userId, [FromBody] ChangeUserRoleResource resource)
    {
        var command = ChangeUserRoleCommandFromResourceAssembler.ToCommandFromResource(userId, resource);
        var user = await userCommandService.Handle(command);
        if (user is null) return NotFound();
        return Ok(UserResourceFromEntityAssembler.ToResourceFromEntity(user));
    }

    [HttpPost("{userId:guid}/deactivation")]
    [Authorize(Roles = Administrator)]
    [ProducesResponseType(typeof(UserResource), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivateUser(Guid userId)
    {
        var user = await userCommandService.Handle(new DeactivateUserCommand(userId));
        if (user is null) return NotFound();
        return Ok(UserResourceFromEntityAssembler.ToResourceFromEntity(user));
    }

    /// <summary>US18: grants the user access to a mining unit of the Operations context.</summary>
    [HttpPost("{userId:guid}/mining-units")]
    [Authorize(Roles = Administrator)]
    [ProducesResponseType(typeof(UserResource), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> GrantMiningUnit(Guid userId, [FromBody] MiningUnitScopeResource resource)
    {
        var user = await userCommandService.Handle(new GrantMiningUnitScopeCommand(userId, resource.MiningUnitId));
        if (user is null) return NotFound();
        return Ok(UserResourceFromEntityAssembler.ToResourceFromEntity(user));
    }

    /// <summary>US18: removes access to a mining unit; the user's history is kept.</summary>
    [HttpDelete("{userId:guid}/mining-units/{miningUnitId:guid}")]
    [Authorize(Roles = Administrator)]
    [ProducesResponseType(typeof(UserResource), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevokeMiningUnit(Guid userId, Guid miningUnitId)
    {
        var user = await userCommandService.Handle(new RevokeMiningUnitScopeCommand(userId, miningUnitId));
        if (user is null) return NotFound();
        return Ok(UserResourceFromEntityAssembler.ToResourceFromEntity(user));
    }
}
