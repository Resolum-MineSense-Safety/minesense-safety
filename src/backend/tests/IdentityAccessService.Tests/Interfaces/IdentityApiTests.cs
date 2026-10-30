using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using IdentityAccessService.Interfaces.REST.Resources;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace IdentityAccessService.Tests.Interfaces;

/// <summary>Starts the API with a known bootstrap administrator.</summary>
public class IdentityApiFactory : WebApplicationFactory<Program>
{
    public const string AdminUsername = "root";
    public const string AdminPassword = "Root-Pass-2026";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("IdentityAccess:BootstrapAdministrator:Username", AdminUsername);
        builder.UseSetting("IdentityAccess:BootstrapAdministrator:Password", AdminPassword);
    }
}

public class IdentityApiTests(IdentityApiFactory factory) : IClassFixture<IdentityApiFactory>
{
    private const string Password = "Turno-Noche-2026";
    private readonly HttpClient _client = factory.CreateClient();

    private async Task<AuthenticatedUserResource> SignInAsync(string username, string password)
    {
        var response = await _client.PostAsJsonAsync("/api/v1/authentication/sign-in", new SignInResource(username, password));
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<AuthenticatedUserResource>())!;
    }

    private async Task<HttpClient> AdministratorClientAsync()
    {
        var session = await SignInAsync(IdentityApiFactory.AdminUsername, IdentityApiFactory.AdminPassword);
        return Authorized(session.AccessToken);
    }

    private HttpClient Authorized(string accessToken)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }

    private async Task<UserResource> SignUpAsync(string role = "Supervisor")
    {
        var username = $"user{Guid.NewGuid():N}"[..14];
        var response = await _client.PostAsJsonAsync("/api/v1/authentication/sign-up",
            new SignUpResource(username, "Carmen Salas", Password, role));
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<UserResource>())!;
    }

    [Fact]
    public async Task SignUp_ThenSignIn_ReturnsTokensForTheUser()
    {
        // Arrange
        var user = await SignUpAsync();

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new SignInResource(user.Username, Password));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var authenticated = await response.Content.ReadFromJsonAsync<AuthenticatedUserResource>();
        Assert.Equal(user.Id, authenticated!.Id);
        Assert.Equal("Bearer", authenticated.TokenType);
        Assert.InRange(authenticated.ExpiresIn, 1, 15 * 60);
        Assert.False(string.IsNullOrEmpty(authenticated.AccessToken));
        Assert.False(string.IsNullOrEmpty(authenticated.RefreshToken));
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
    public async Task SignUp_AsAdministratorWithoutToken_ReturnsForbidden()
    {
        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/authentication/sign-up",
            new SignUpResource("intruder", "Intruder", Password, "Administrator"));

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task SignUp_WithDuplicateUsername_ReturnsConflict()
    {
        // Arrange
        var user = await SignUpAsync();

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/authentication/sign-up",
            new SignUpResource(user.Username, "Other Person", Password, "Operator"));

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task SignIn_WithWrongPassword_ReturnsUnauthorizedWithoutTokens()
    {
        // Arrange
        var user = await SignUpAsync();

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/authentication/sign-in", new SignInResource(user.Username, "wrong-password"));

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.DoesNotContain("accessToken", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Refresh_WithValidToken_ReturnsNewPairAndInvalidatesTheOldOne()
    {
        // Arrange
        var user = await SignUpAsync();
        var session = await SignInAsync(user.Username, Password);

        // Act
        var refreshed = await _client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshTokenResource(session.RefreshToken));
        var reused = await _client.PostAsJsonAsync("/api/v1/authentication/refresh", new RefreshTokenResource(session.RefreshToken));

        // Assert
        Assert.Equal(HttpStatusCode.OK, refreshed.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, reused.StatusCode);
    }

    [Fact]
    public async Task SignOut_ThenRefresh_ReturnsUnauthorized()
    {
        // Arrange
        var user = await SignUpAsync();
        var session = await SignInAsync(user.Username, Password);

        // Act
        var signOut = await _client.PostAsJsonAsync("/api/v1/authentication/sign-out", new RefreshTokenResource(session.RefreshToken));
        var refresh = await _client.PostAsJsonAsync("/api/v1/authentication/refresh", new RefreshTokenResource(session.RefreshToken));

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, signOut.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
    }

    [Fact]
    public async Task GetMe_WithToken_ReturnsTheSignedInUser()
    {
        // Arrange
        var user = await SignUpAsync();
        var session = await SignInAsync(user.Username, Password);

        // Act
        var me = await Authorized(session.AccessToken).GetFromJsonAsync<UserResource>("/api/v1/users/me");

        // Assert
        Assert.Equal(user.Id, me!.Id);
        Assert.Equal("Supervisor", me.Role);
    }

    [Fact]
    public async Task GetMe_WithoutOrWithTamperedToken_ReturnsUnauthorized()
    {
        // Arrange
        var user = await SignUpAsync();
        var session = await SignInAsync(user.Username, Password);
        var tampered = session.AccessToken[..^4] + "AAAA";

        // Act
        var anonymous = await _client.GetAsync("/api/v1/users/me");
        var forged = await Authorized(tampered).GetAsync("/api/v1/users/me");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, forged.StatusCode);
    }

    [Fact]
    public async Task GetMe_ForDeactivatedUserWithValidToken_ReturnsForbidden()
    {
        // Arrange
        var admin = await AdministratorClientAsync();
        var user = await SignUpAsync();
        var session = await SignInAsync(user.Username, Password);
        await admin.PostAsync($"/api/v1/users/{user.Id}/deactivation", null);

        // Act
        var response = await Authorized(session.AccessToken).GetAsync("/api/v1/users/me");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AdministrationRoutes_WithoutTokenOrWithoutRole_AreRejected()
    {
        // Arrange
        var user = await SignUpAsync("SafetyManager");
        var session = await SignInAsync(user.Username, Password);

        // Act
        var anonymous = await _client.GetAsync("/api/v1/users");
        var notAdministrator = await Authorized(session.AccessToken).GetAsync("/api/v1/users");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, notAdministrator.StatusCode);
    }

    [Fact]
    public async Task CreateUser_AsAdministrator_ReturnsCreatedAndConflictOnDuplicate()
    {
        // Arrange
        var admin = await AdministratorClientAsync();
        var resource = new SignUpResource($"admin{Guid.NewGuid():N}"[..13], "Lucia Rojas", Password, "Administrator");

        // Act
        var created = await admin.PostAsJsonAsync("/api/v1/users", resource);
        var duplicate = await admin.PostAsJsonAsync("/api/v1/users", resource);

        // Assert
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal("Administrator", (await created.Content.ReadFromJsonAsync<UserResource>())!.Role);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
    }

    [Fact]
    public async Task GetUsers_AllByRoleAndById_ReturnUsers()
    {
        // Arrange
        var admin = await AdministratorClientAsync();
        var user = await SignUpAsync("SafetyManager");

        // Act
        var all = await admin.GetFromJsonAsync<List<UserResource>>("/api/v1/users");
        var byRole = await admin.GetFromJsonAsync<List<UserResource>>("/api/v1/users?role=SafetyManager");
        var found = await admin.GetAsync($"/api/v1/users/{user.Id}");
        var missing = await admin.GetAsync($"/api/v1/users/{Guid.NewGuid()}");

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
        var admin = await AdministratorClientAsync();
        var user = await SignUpAsync();

        // Act
        var changed = await admin.PutAsJsonAsync($"/api/v1/users/{user.Id}/role", new ChangeUserRoleResource("Administrator"));
        var deactivated = await admin.PostAsync($"/api/v1/users/{user.Id}/deactivation", null);

        // Assert
        Assert.Equal("Administrator", (await changed.Content.ReadFromJsonAsync<UserResource>())!.Role);
        Assert.False((await deactivated.Content.ReadFromJsonAsync<UserResource>())!.IsActive);
    }

    [Fact]
    public async Task MiningUnitScopes_AreGrantedRevokedAndIssuedInTheToken()
    {
        // Arrange
        var admin = await AdministratorClientAsync();
        var user = await SignUpAsync();
        var miningUnitId = Guid.NewGuid();

        // Act
        var granted = await admin.PostAsJsonAsync($"/api/v1/users/{user.Id}/mining-units", new MiningUnitScopeResource(miningUnitId));
        var session = await SignInAsync(user.Username, Password);
        var revoked = await admin.DeleteAsync($"/api/v1/users/{user.Id}/mining-units/{miningUnitId}");

        // Assert
        Assert.Equal([miningUnitId], (await granted.Content.ReadFromJsonAsync<UserResource>())!.MiningUnitIds);
        Assert.Equal([miningUnitId], session.MiningUnitIds);
        Assert.Empty((await revoked.Content.ReadFromJsonAsync<UserResource>())!.MiningUnitIds);
    }

    [Fact]
    public async Task UserOperations_OnUnknownUser_ReturnNotFound()
    {
        // Arrange
        var admin = await AdministratorClientAsync();

        // Act
        var role = await admin.PutAsJsonAsync($"/api/v1/users/{Guid.NewGuid()}/role", new ChangeUserRoleResource("Operator"));
        var deactivate = await admin.PostAsync($"/api/v1/users/{Guid.NewGuid()}/deactivation", null);
        var scope = await admin.PostAsJsonAsync($"/api/v1/users/{Guid.NewGuid()}/mining-units", new MiningUnitScopeResource(Guid.NewGuid()));

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, role.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, deactivate.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, scope.StatusCode);
    }

    [Fact]
    public async Task Jwks_PublishesTheRsaSigningKey()
    {
        // Act
        var jwks = await _client.GetFromJsonAsync<JsonElement>("/.well-known/jwks.json");

        // Assert
        var key = jwks.GetProperty("keys")[0];
        Assert.Equal("RSA", key.GetProperty("kty").GetString());
        Assert.Equal("RS256", key.GetProperty("alg").GetString());
        Assert.False(key.TryGetProperty("d", out _));
    }
}
