using System.Net;
using System.Net.Http.Json;
using IdentityAccessService.Interfaces.REST.Resources;
using Microsoft.AspNetCore.Mvc.Testing;

namespace IdentityAccessService.Tests.Interfaces;

public class IdentityApiTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private const string Password = "Turno-Noche-2026";
    private readonly HttpClient _client = factory.CreateClient();

    private async Task<UserResource> SignUpAsync(string role = "Supervisor")
    {
        var username = $"user{Guid.NewGuid():N}"[..14];
        var response = await _client.PostAsJsonAsync("/api/v1/authentication/sign-up",
            new SignUpResource(username, "Carmen Salas", Password, role));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<UserResource>())!;
    }

    [Fact]
    public async Task SignUp_ThenSignIn_ReturnsAuthenticatedUser()
    {
        // Arrange
        var user = await SignUpAsync();

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/authentication/sign-in", new SignInResource(user.Username, Password));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var authenticated = await response.Content.ReadFromJsonAsync<AuthenticatedUserResource>();
        Assert.Equal(user.Id, authenticated!.Id);
    }

    [Fact]
    public async Task SignUp_WithShortPassword_ReturnsUnprocessableEntity()
    {
        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/authentication/sign-up",
            new SignUpResource("shortpass", "Ana Choque", "123", "Operator"));

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task SignIn_WithWrongPassword_ReturnsUnprocessableEntity()
    {
        // Arrange
        var user = await SignUpAsync();

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/authentication/sign-in", new SignInResource(user.Username, "wrong-password"));

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task GetUsers_AllByRoleAndById_ReturnUsers()
    {
        // Arrange
        var user = await SignUpAsync("SafetyManager");

        // Act
        var all = await _client.GetFromJsonAsync<List<UserResource>>("/api/v1/users");
        var byRole = await _client.GetFromJsonAsync<List<UserResource>>("/api/v1/users?role=SafetyManager");
        var found = await _client.GetAsync($"/api/v1/users/{user.Id}");
        var missing = await _client.GetAsync($"/api/v1/users/{Guid.NewGuid()}");

        // Assert
        Assert.Contains(all!, u => u.Id == user.Id);
        Assert.All(byRole!, u => Assert.Equal("SafetyManager", u.Role));
        Assert.Equal(HttpStatusCode.OK, found.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    [Fact]
    public async Task ChangeRole_AndDeactivate_UpdateTheUser()
    {
        // Arrange
        var user = await SignUpAsync();

        // Act
        var changed = await _client.PutAsJsonAsync($"/api/v1/users/{user.Id}/role", new ChangeUserRoleResource("Administrator"));
        var deactivated = await _client.PostAsync($"/api/v1/users/{user.Id}/deactivation", null);

        // Assert
        Assert.Equal("Administrator", (await changed.Content.ReadFromJsonAsync<UserResource>())!.Role);
        Assert.False((await deactivated.Content.ReadFromJsonAsync<UserResource>())!.IsActive);
    }

    [Fact]
    public async Task UserOperations_OnUnknownUser_ReturnNotFound()
    {
        // Act
        var role = await _client.PutAsJsonAsync($"/api/v1/users/{Guid.NewGuid()}/role", new ChangeUserRoleResource("Operator"));
        var deactivate = await _client.PostAsync($"/api/v1/users/{Guid.NewGuid()}/deactivation", null);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, role.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, deactivate.StatusCode);
    }
}
