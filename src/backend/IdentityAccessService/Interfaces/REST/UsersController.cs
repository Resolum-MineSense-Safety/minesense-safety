using System.Net.Mime;
using IdentityAccessService.Domain.Model.Aggregates;
using IdentityAccessService.Domain.Model.Commands;
using IdentityAccessService.Domain.Model.Queries;
using IdentityAccessService.Domain.Services;
using IdentityAccessService.Interfaces.REST.Resources;
using IdentityAccessService.Interfaces.REST.Transform;
using Microsoft.AspNetCore.Mvc;

namespace IdentityAccessService.Interfaces.REST;

[ApiController]
[Route("api/v1/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
public class UsersController(
    IUserCommandService userCommandService,
    IUserQueryService userQueryService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetUsers([FromQuery] string? role)
    {
        IEnumerable<User> users = string.IsNullOrWhiteSpace(role)
            ? await userQueryService.Handle(new GetAllUsersQuery())
            : await userQueryService.Handle(new GetUsersByRoleQuery(UserRoleParser.Parse(role)));
        return Ok(users.Select(UserResourceFromEntityAssembler.ToResourceFromEntity));
    }

    [HttpGet("{userId:guid}")]
    public async Task<IActionResult> GetUserById(Guid userId)
    {
        var user = await userQueryService.Handle(new GetUserByIdQuery(userId));
        if (user is null) return NotFound();
        return Ok(UserResourceFromEntityAssembler.ToResourceFromEntity(user));
    }

    [HttpPut("{userId:guid}/role")]
    public async Task<IActionResult> ChangeUserRole(Guid userId, [FromBody] ChangeUserRoleResource resource)
    {
        var command = ChangeUserRoleCommandFromResourceAssembler.ToCommandFromResource(userId, resource);
        var user = await userCommandService.Handle(command);
        if (user is null) return NotFound();
        return Ok(UserResourceFromEntityAssembler.ToResourceFromEntity(user));
    }

    [HttpPost("{userId:guid}/deactivation")]
    public async Task<IActionResult> DeactivateUser(Guid userId)
    {
        var user = await userCommandService.Handle(new DeactivateUserCommand(userId));
        if (user is null) return NotFound();
        return Ok(UserResourceFromEntityAssembler.ToResourceFromEntity(user));
    }
}
