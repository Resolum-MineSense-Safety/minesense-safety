using System.Net;
using System.Net.Http.Json;
using OperationsService.Interfaces.REST.Resources;

namespace OperationsService.Tests.Interfaces;

/// <summary>The assignment endpoints require a token, and the writing ones a supervisor or administrator.</summary>
public class OperatorAssignmentsAuthorizationTests(OperationsApiFactory factory) : IClassFixture<OperationsApiFactory>
{
    private static readonly DateOnly October1 = new(2026, 10, 1);

    [Theory]
    [InlineData("/api/v1/operator-assignments/active?operatorId=00000000-0000-0000-0000-000000000001")]
    [InlineData("/api/v1/operator-assignments?operatorId=00000000-0000-0000-0000-000000000001")]
    [InlineData("/api/v1/operator-assignments/context?operatorId=00000000-0000-0000-0000-000000000001")]
    [InlineData("/api/v1/operator-assignments/00000000-0000-0000-0000-000000000001")]
    public async Task Get_WithoutToken_ReturnsUnauthorized(string url)
    {
        // Arrange
        var client = factory.AnonymousClient();

        // Act
        var response = await client.GetAsync(url);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PostAssignment_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = factory.AnonymousClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/operator-assignments",
            new CreateAssignmentResource(Guid.NewGuid(), Guid.NewGuid(), "Day", October1, null));

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("Operator")]
    [InlineData("SafetyManager")]
    public async Task PostAssignment_WithRoleThatCannotManageAssignments_ReturnsForbidden(string role)
    {
        // Arrange
        var client = factory.ClientWithRole(role);

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/operator-assignments",
            new CreateAssignmentResource(Guid.NewGuid(), Guid.NewGuid(), "Day", October1, null));

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetActiveAssignment_WithAnyAuthenticatedRole_IsAllowed()
    {
        // Arrange
        var client = factory.ClientWithRole("Operator");

        // Act
        var response = await client.GetAsync("/api/v1/operator-assignments/active?operatorId=00000000-0000-0000-0000-000000000001");

        // Assert: authorised, and there is simply no active assignment for that operator
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
