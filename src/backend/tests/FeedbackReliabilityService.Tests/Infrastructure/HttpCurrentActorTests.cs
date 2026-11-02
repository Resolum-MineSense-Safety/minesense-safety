using System.Security.Claims;
using FeedbackReliabilityService.Application.Internal;
using FeedbackReliabilityService.Infrastructure.Security;
using Microsoft.AspNetCore.Http;

namespace FeedbackReliabilityService.Tests.Infrastructure;

public class HttpCurrentActorTests
{
    private static HttpCurrentActor Actor(string role, params Claim[] extra)
    {
        var claims = new List<Claim> { new("sub", Guid.NewGuid().ToString()), new("role", role) };
        claims.AddRange(extra);
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer", "sub", "role"))
        };
        return new HttpCurrentActor(new HttpContextAccessor { HttpContext = context });
    }

    [Theory]
    [InlineData("SafetyManager")]
    [InlineData("Administrator")]
    public void IsInRole_IdentityRolesThatReadMetrics_ActAsManager(string role)
    {
        Assert.True(Actor(role).IsInRole("Manager"));
    }

    [Fact]
    public void IsInRole_Operator_IsNotManagerNorService()
    {
        var actor = Actor("Operator");

        Assert.False(actor.IsInRole("Manager"));
        Assert.False(actor.IsInRole("Service"));
    }

    [Fact]
    public void RequireOperation_MiningUnitClaimOfIdentity_GrantsTheOperation()
    {
        var operationId = Guid.NewGuid();
        var actor = Actor("Supervisor", new Claim("mining_unit", operationId.ToString()));

        actor.RequireOperation(operationId);
        Assert.Throws<FeedbackForbiddenException>(() => actor.RequireOperation(Guid.NewGuid()));
    }
}
